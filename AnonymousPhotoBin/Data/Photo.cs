using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnonymousPhotoBin.Data {
    public class Photo {
        public int PhotoId { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public DateTime? TakenAt { get; set; }

        public DateTimeOffset UploadedAt { get; set; }

        public string OriginalFilename { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public int PhotoDataId { get; set; }

        [Column(TypeName = "binary"), MaxLength(32)]
        public byte[] SHA256 { get; set; }

        public int ThumbnailDataId { get; set; }

        [ForeignKey(nameof(PhotoDataId))]
        public PhotoData PhotoData { get; set; }

        [ForeignKey(nameof(ThumbnailDataId))]
        public PhotoData ThumbnailData { get; set; }
    }
}
