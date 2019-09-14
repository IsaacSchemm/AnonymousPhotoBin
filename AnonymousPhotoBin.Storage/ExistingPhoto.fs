namespace AnonymousPhotoBin.Storage

open System

type IExistingPhoto =
    abstract member Id: Guid
    abstract member TakenAt: Nullable<DateTimeOffset>
    abstract member UploadedAt: Nullable<DateTimeOffset>
    abstract member OriginalFilename: string
    abstract member UserName: string
    abstract member Category: string
    abstract member Size: int64