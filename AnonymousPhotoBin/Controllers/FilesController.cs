using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using MetadataExtractor;
using System.Globalization;

namespace AnonymousPhotoBin.Controllers {
    [Route("api/[controller]")]
    public class FilesController : Controller {
        // GET api/files
        [HttpGet]
        public IEnumerable<string> Get() {
            throw new NotImplementedException();
        }

        // GET api/files/5
        [HttpGet("{id}")]
        public string Get(int id) {
            throw new NotImplementedException();
        }

        public class UploadedFile {
            public string url, thumbnailUrl, name, type;
            public long size;
        }

        // POST api/files
        [HttpPost]
        public async Task<object> Post(List<IFormFile> files, string timezone = null) {
            List<UploadedFile> l = new List<UploadedFile>();
            foreach (var file in files) {
                if (file.FileName == "mark.png") throw new Exception("test");
                using (var ms = new MemoryStream()) {
                    await file.OpenReadStream().CopyToAsync(ms);
                    byte[] data = ms.ToArray();
                    string f = Guid.NewGuid().ToString() + ".png";
                    System.IO.File.WriteAllBytes(f, data);
                    //System.Diagnostics.Process.Start("explorer", f);
                    await Task.Delay(3000);
                    System.IO.File.Delete(f);
                }
                l.Add(new UploadedFile {
                    url = "https://s0.2mdn.net/5585042/320x100_WS_2016_3_.png",
                    thumbnailUrl = "https://s0.2mdn.net/5585042/320x100_WS_2016_3_.png",
                    name = file.FileName,
                    type = file.ContentType,
                    size = file.Length
                });
            }
            return new { files = l };
        }

        // DELETE api/files/5
        [HttpDelete("{id}")]
        public void Delete(int id) {
            throw new NotImplementedException();
        }
    }
}
