using AnonymousPhotoBin.Storage;
using System;

namespace AnonymousPhotoBin
{
    public class DataAndContentType : IDataAndContentType
    {
        public byte[] Data { get; private set; }
        public string ContentType { get; private set; }

        public DataAndContentType(byte[] data, string contentType)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        }
    }

    public class NewPhoto : INewPhoto
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public IDataAndContentType Photo { get; set; }
        public IDataAndContentType Thumbnail { get; set; }
        public DateTimeOffset? TakenAt { get; set; }
        public string OriginalFilename { get; set; }
        public string UserName { get; set; }
        public string Category { get; set; }
    }
}
