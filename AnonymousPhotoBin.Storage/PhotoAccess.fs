namespace AnonymousPhotoBin.Storage

open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob
open FSharp.Control.Tasks.V2
open System.Threading.Tasks
open System
open FSharp.Control

type IPhotoAccessCredentials =
    abstract member ConnectionString: string
    abstract member ContainerName: string

module PhotoAccess =
    let GetBlobContainerAsync (c: IPhotoAccessCredentials) = task {
        let client =
            c.ConnectionString
            |> CloudStorageAccount.Parse
            |> BlobAccountExtensions.CreateCloudBlobClient

        let container = client.GetContainerReference c.ContainerName
        do! container.CreateIfNotExistsAsync() :> Task

        let permissions = new BlobContainerPermissions(PublicAccess = BlobContainerPublicAccessType.Blob)
        do! container.SetPermissionsAsync permissions

        return container
    }

    let AsyncGetPhotos c = asyncSeq {
        let! container = GetBlobContainerAsync c |> Async.AwaitTask
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

    let GetPhotosAsync c limit =
        AsyncGetPhotos c
        |> AsyncSeq.take limit
        |> AsyncSeq.toArrayAsync
        |> Async.StartAsTask

    let GetFullUriAsync (c: IPhotoAccessCredentials) (id: Guid) = task {
        let! container = GetBlobContainerAsync c
        let blob = sprintf "full-%O" id |> container.GetBlockBlobReference
        return blob.Uri
    }

    let GetThumbnailUriAsync (c: IPhotoAccessCredentials) (id: Guid) = task {
        let! container = GetBlobContainerAsync c
        let blob = sprintf "thumb-%O" id |> container.GetBlockBlobReference
        return blob.Uri
    }