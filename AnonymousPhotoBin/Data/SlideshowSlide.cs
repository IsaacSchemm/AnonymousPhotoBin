using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnonymousPhotoBin.Data {
    public class SlideshowSlide {
        public int SlideshowSlideId { get; set; }

        public Guid SlideshowId { get; set; }

        public Guid FileMetadataId { get; set; }

        [ForeignKey(nameof(FileMetadataId))]
        public FileMetadata FileMetadata { get; set; }
    }
}
