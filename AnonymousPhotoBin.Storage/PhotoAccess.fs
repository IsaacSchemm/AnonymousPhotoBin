namespace AnonymousPhotoBin.Storage

open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob
open System
open FSharp.Control
open System.Collections.Generic

type FullPhotoBlob = {
    Id: Guid
    Blob: CloudBlockBlob
}

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

    let ToFullPhotoBlob (blob: CloudBlockBlob) =
        if blob.Name.StartsWith("full-") then
            match blob.Name.Replace("full-", "") |> Guid.TryParse with
            | (true, g) -> Some { Id = g; Blob = blob }
            | (false, _) -> None
        else
            None

    let AsyncGetFullPhotoBlobs container =
        AsyncGetAllBlobs container
        |> AsyncSeq.choose ToFullPhotoBlob

    let GetFullPhotoBlobsAsync c limit =
        AsyncGetFullPhotoBlobs c
        |> AsyncSeq.take limit
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let GetValueIfAny (key: 'a) (dict: IDictionary<'a, 'b>) =
        match dict.TryGetValue key with
        | (true, x) -> Some x
        | (false, _) -> None

    let AsyncToExistingPhotoMetadata (photo: FullPhotoBlob) = async {
        let thumb = sprintf "thumb-%O" photo.Id |> photo.Blob.Container.GetBlockBlobReference

        do! photo.Blob.FetchAttributesAsync() |> Async.AwaitTask
        let metadata = photo.Blob.Metadata

        return Some {
            Id = photo.Id
            TakenAt = metadata |> GetValueIfAny "TakenAt" |> Option.map DateTimeOffset.Parse |> Option.toNullable
            UploadedAt = photo.Blob.Properties.LastModified
            OriginalFilename = metadata |> GetValueIfAny "OriginalFilename" |> Option.toObj
            UserName = metadata |> GetValueIfAny "UserName" |> Option.toObj
            Category = metadata |> GetValueIfAny "Category" |> Option.toObj
            Size = photo.Blob.Properties.Length
            Url = photo.Blob.Uri.AbsoluteUri
            ThumbnailUrl = thumb.Uri.AbsoluteUri
        }
    }

    let AsyncGetExistingPhotoMetadata c =
        AsyncGetFullPhotoBlobs c
        |> AsyncSeq.chooseAsync AsyncToExistingPhotoMetadata
    
    let GetExistingPhotoMetadataAsync c limit =
        AsyncGetExistingPhotoMetadata c
        |> AsyncSeq.take limit
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let AsyncGetExistingPhotoMetadataByIds (container: CloudBlobContainer) (ids: seq<Guid>) =
        ids
        |> Seq.map (sprintf "full-%O")
        |> Seq.map container.GetBlockBlobReference
        |> AsyncSeq.ofSeq
        |> AsyncSeq.choose ToFullPhotoBlob
        |> AsyncSeq.chooseAsync AsyncToExistingPhotoMetadata

    let GetExistingPhotoMetadataByIdsAsync container ids =
        AsyncGetExistingPhotoMetadataByIds container ids
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let AsyncDeletePhoto (container: CloudBlobContainer) (id: Guid) = async {
        let full =
            id
            |> sprintf "full-%O"
            |> container.GetBlockBlobReference
        let thumb =
            id
            |> sprintf "full-%O"
            |> container.GetBlockBlobReference
        for blob in [full; thumb] do
            do! blob.DeleteIfExistsAsync() |> Async.AwaitTask |> Async.Ignore
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
        if isNull photo.Photo then
            nullArg "photo.Photo"

        if not (isNull photo.Thumbnail) then
            let! thumb = photo.Id |> sprintf "thumb-%O" |> AsyncGetBlobIfDoesNotExist container
            thumb.Properties.ContentType <- photo.Thumbnail.ContentType
            do! AsyncUploadBlob thumb photo.Thumbnail.Data

        let! full = photo.Id |> sprintf "full-%O" |> AsyncGetBlobIfDoesNotExist container
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

        let! now_exists =
            { Id = photo.Id; Blob = full }
            |> AsyncToExistingPhotoMetadata
        return Option.get now_exists
    }

    let UploadPhotoAsync container photo =
        AsyncUploadPhoto container photo
        |> Async.StartAsTask