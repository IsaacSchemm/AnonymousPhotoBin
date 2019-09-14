namespace AnonymousPhotoBin.Storage

open System

[<AllowNullLiteral>]
type IDataAndContentType =
    abstract member Data: byte[]
    abstract member ContentType: string

type INewPhoto =
    abstract member Photo: IDataAndContentType
    abstract member Thumbnail: IDataAndContentType
    abstract member Id: Guid
    abstract member TakenAt: Nullable<DateTimeOffset>
    abstract member OriginalFilename: string
    abstract member UserName: string
    abstract member Category: string