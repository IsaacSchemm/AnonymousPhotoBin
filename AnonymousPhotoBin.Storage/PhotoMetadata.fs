namespace AnonymousPhotoBin.Storage

open System

type BlobMetadata = {
    TakenAt: DateTimeOffset option
    OriginalFilename: string
    UserName: string option
    Category: string option
}

type PhotoMetadata = {
    Id: Guid
    TakenAt: DateTimeOffset option
    UploadedAt: DateTimeOffset
    OriginalFilename: string
    UserName: string
    Category: string option
    Size: int
}