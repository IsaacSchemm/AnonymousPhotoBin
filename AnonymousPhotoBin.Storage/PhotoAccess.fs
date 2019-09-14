namespace AnonymousPhotoBin.Storage

open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob
open System
open FSharp.Control

module PhotoAccess =
    //let AsyncGetBlobContainer connectionString containerName = async {
    //    let client =
    //       connectionString
    //        |> CloudStorageAccount.Parse
    //        |> BlobAccountExtensions.CreateCloudBlobClient

    //    let container = client.GetContainerReference containerName
    //    do! container.CreateIfNotExistsAsync() |> Async.AwaitTask |> Async.Ignore

    //    let permissions = new BlobContainerPermissions(PublicAccess = BlobContainerPublicAccessType.Blob)
    //    do! container.SetPermissionsAsync permissions |> Async.AwaitTask

    //    return container
    //}

    //let GetBlobContainerAsync connectionString containerName =
    //    AsyncGetBlobContainer connectionString containerName
    //    |> Async.StartAsTask

    let GuidToString (g: Guid) = g.ToString()

    let AsyncGetBlobs (container: CloudBlobContainer) = asyncSeq {
        let mutable token: BlobContinuationToken = null
        let mutable finished = false
        while not finished do
            let! segment = container.ListBlobsSegmentedAsync token |> Async.AwaitTask
            for b in segment.Results do
                match b with
                | :? CloudBlockBlob as o -> yield o
                | _ -> ()
            match segment.ContinuationToken with
            | null -> finished <- true
            | t -> token <- t
    }

    let GetBlobsAsync c limit =
        AsyncGetBlobs c
        |> AsyncSeq.take limit
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let GetMetadataValue (name: string) (blob: CloudBlockBlob) =
        match blob.Metadata.TryGetValue name with
        | (true, x) -> Some x
        | (false, _) -> None

    let GetIdFromBlob (blob: CloudBlockBlob) =
        if blob.Name.StartsWith("full-") then
            match blob.Name.Replace("full-", "") |> Guid.TryParse with
            | (true, g) -> Some g
            | (false, _) -> None
        else
            None

    let AsyncGetExistingPhotoFromBlob (blob: CloudBlockBlob) = async {
        match GetIdFromBlob blob with
        | Some g ->
            do! blob.FetchAttributesAsync() |> Async.AwaitTask
            let thumb = blob.Name.Replace("full-", "thumb-") |> blob.Container.GetBlockBlobReference
            return Some {
                Id = g
                TakenAt = blob |> GetMetadataValue "TakenAt" |> Option.map DateTimeOffset.Parse |> Option.toNullable
                UploadedAt = blob.Properties.LastModified
                OriginalFilename = blob |> GetMetadataValue "OriginalFilename" |> Option.toObj
                UserName = blob |> GetMetadataValue "UserName" |> Option.toObj
                Category = blob |> GetMetadataValue "Category" |> Option.toObj
                Size = blob.Properties.Length
                Url = blob.Uri.AbsoluteUri
                ThumbnailUrl = thumb.Uri.AbsoluteUri
            }
        | None ->
            return None
    }

    let AsyncGetExistingPhotos c =
        AsyncGetBlobs c
        |> AsyncSeq.chooseAsync AsyncGetExistingPhotoFromBlob
    
    let GetExistingPhotosAsync c limit =
        AsyncGetExistingPhotos c
        |> AsyncSeq.take limit
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let AsyncGetExistingPhotosByIds (container: CloudBlobContainer) (ids: seq<Guid>) =
        ids
        |> Seq.map GuidToString
        |> Seq.map (sprintf "full-%s")
        |> Seq.map container.GetBlockBlobReference
        |> AsyncSeq.ofSeq
        |> AsyncSeq.chooseAsync AsyncGetExistingPhotoFromBlob

    let GetExistingPhotosByIdsAsync container ids =
        AsyncGetExistingPhotosByIds container ids
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask
        
    let AsyncGetExistingPhotoById (container: CloudBlobContainer) (id: Guid) =
        Seq.singleton id
        |> AsyncGetExistingPhotosByIds container
        |> AsyncSeq.tryFirst

    let AsyncDeletePhoto (container: CloudBlobContainer) (id: Guid) = async {
        let full =
            id
            |> GuidToString
            |> sprintf "full-%s"
            |> container.GetBlockBlobReference
        let thumb =
            id
            |> GuidToString
            |> sprintf "full-%s"
            |> container.GetBlockBlobReference
        for blob in [full; thumb] do
            do! blob.DeleteIfExistsAsync() |> Async.AwaitTask |> Async.Ignore
    }

    let DeletePhotoAsync container id =
        AsyncDeletePhoto container id
        |> Async.StartAsTask

    let AsyncGetBlobIfExistsNot (container: CloudBlobContainer) (name: string) = async {
        let blob = container.GetBlockBlobReference name
        let! exists = blob.ExistsAsync() |> Async.AwaitTask
        return if exists
            then failwithf "Blob already exists"
            else blob
    }

    let AsyncUploadBlob (blob: CloudBlockBlob) (data: byte[]) = async {
        do! blob.UploadFromByteArrayAsync(data, 0, data.Length) |> Async.AwaitTask
    }

    let AsyncUploadPhoto (container: CloudBlobContainer) (photo: INewPhoto) = async {
        if isNull photo.Photo then
            nullArg "photo.Photo"

        if not (isNull photo.Thumbnail) then
            let! thumb = photo.Id |> GuidToString |> sprintf "thumb-%s" |> AsyncGetBlobIfExistsNot container
            thumb.Properties.ContentType <- photo.Thumbnail.ContentType
            do! AsyncUploadBlob thumb photo.Thumbnail.Data

        let! full = photo.Id |> GuidToString |> sprintf "full-%O" |> AsyncGetBlobIfExistsNot container
        full.Properties.ContentType <- photo.Photo.ContentType
        if photo.TakenAt.HasValue then
            full.Metadata.Add("TakenAt", photo.TakenAt.Value.ToString("o"))
        if not (isNull photo.OriginalFilename) then
            full.Metadata.Add("OriginalFilename", photo.OriginalFilename)
        if not (isNull photo.UserName) then
            full.Metadata.Add("UserName", photo.UserName)
        if not (isNull photo.Category) then
            full.Metadata.Add("Category", photo.Category)
        do! AsyncUploadBlob full photo.Photo.Data

        return! AsyncGetExistingPhotoFromBlob full
    }

    let UploadPhotoAsync container photo =
        AsyncUploadPhoto container photo
        |> Async.StartAsTask