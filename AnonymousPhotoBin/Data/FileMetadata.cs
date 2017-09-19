using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnonymousPhotoBin.Data {
    public class FileMetadata {
        public Guid FileMetadataId { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public DateTime? TakenAt { get; set; }

        public DateTimeOffset UploadedAt { get; set; }

        public string OriginalFilename { get; set; }

        public string UserName { get; set; }

        public string Category { get; set; }

        [Column(TypeName = "binary(32)")]
        public byte[] Sha256 { get; set; }

        public string ContentType { get; set; }

        public int FileDataId { get; set; }

        public int? JpegThumbnailId { get; set; }

        [ForeignKey(nameof(FileDataId))]
        public FileData FileData { get; set; }

        [ForeignKey(nameof(JpegThumbnailId))]
        public FileData JpegThumbnail { get; set; }

        public string Url => "/api/files/" + FileMetadataId;

        public string ThumbnailUrl => "/api/thumbnails/" + FileMetadataId;
    }
}
