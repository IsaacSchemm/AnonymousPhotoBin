namespace AnonymousPhotoBin.Storage

open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob
open System
open FSharp.Control
open System.Collections.Generic

module PhotoAccess =
    let AsyncGetAllBlobs (container: CloudBlobContainer) = asyncSeq {
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

    let GetAllBlobsAsync c limit =
        AsyncGetAllBlobs c
        |> AsyncSeq.take limit
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let GetValueIfAny (key: 'a) (dict: IDictionary<'a, 'b>) =
        match dict.TryGetValue key with
        | (true, x) -> Some x
        | (false, _) -> None

    let AsyncToExistingPhotoMetadata (blob: CloudBlockBlob) = async {
        sprintf "Fetching metadata for %s" blob.Name |> System.Diagnostics.Debug.WriteLine
        do! blob.FetchAttributesAsync() |> Async.AwaitTask
        let metadata = blob.Metadata

        return
            match blob.Name |> Guid.TryParse with
            | (false, _) -> None
            | (true, id) -> Some {
                Id = id
                TakenAt = metadata |> GetValueIfAny "TakenAt" |> Option.map DateTimeOffset.Parse |> Option.toNullable
                UploadedAt = blob.Properties.LastModified
                OriginalFilename = metadata |> GetValueIfAny "OriginalFilename" |> Option.toObj
                UserName = metadata |> GetValueIfAny "UserName" |> Option.toObj
                Category = metadata |> GetValueIfAny "Category" |> Option.toObj
                Size = blob.Properties.Length
                Url = blob.Uri.AbsoluteUri
            }
    }

    let AsyncGetExistingPhotoMetadata c =
        AsyncGetAllBlobs c
        |> AsyncSeq.chooseAsync AsyncToExistingPhotoMetadata
    
    let GetExistingPhotoMetadataAsync c limit =
        AsyncGetExistingPhotoMetadata c
        |> AsyncSeq.take limit
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let AsyncGetExistingPhotoMetadataByIds (container: CloudBlobContainer) (ids: seq<Guid>) =
        ids
        |> Seq.map (sprintf "%O")
        |> Seq.map container.GetBlockBlobReference
        |> AsyncSeq.ofSeq
        |> AsyncSeq.chooseAsync AsyncToExistingPhotoMetadata

    let GetExistingPhotoMetadataByIdsAsync container ids =
        AsyncGetExistingPhotoMetadataByIds container ids
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let AsyncDeletePhoto (container: CloudBlobContainer) (id: Guid) = async {
        let blob =
            id
            |> sprintf "%O"
            |> container.GetBlockBlobReference
        do! blob.DeleteIfExistsAsync()
            |> Async.AwaitTask
            |> Async.Ignore
    }

    let DeletePhotoAsync container id =
        AsyncDeletePhoto container id
        |> Async.StartAsTask

    let AsyncGetBlobIfDoesNotExist (container: CloudBlobContainer) (name: string) = async {
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
        let! full = photo.Id |> sprintf "%O" |> AsyncGetBlobIfDoesNotExist container
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

        let! now_exists = AsyncToExistingPhotoMetadata full
        return Option.get now_exists
    }

    let UploadPhotoAsync container photo =
        AsyncUploadPhoto container photo
        |> Async.StartAsTask