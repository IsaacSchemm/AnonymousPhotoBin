namespace AnonymousPhotoBin.Storage

open System
open System.IO

type ExistingPhotoMetadata = {
    Id: Guid
    TakenAt: Nullable<DateTimeOffset>
    UploadedAt: Nullable<DateTimeOffset>
    OriginalFilename: string
    UserName: string
    Category: string
    Size: int64
    Url: string
} with
    member this.NewFilename =
        let originalFilename = this.OriginalFilename |> Option.ofObj |> Option.defaultValue "untitled.jpg"
        let basename = Path.GetFileNameWithoutExtension originalFilename
        let ext = Path.GetExtension originalFilename
        sprintf "%s (%O)%s" basename this.Id ext