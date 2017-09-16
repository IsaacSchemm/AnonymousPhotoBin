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

        // POST api/files
        [HttpPost]
        public async Task<IEnumerable<string>> Post(List<IFormFile> files, string timezone = null) {
            foreach (var file in files) {
                using (var ms = new MemoryStream()) {
                    await file.OpenReadStream().CopyToAsync(ms);
                    byte[] data = ms.ToArray();
                    System.IO.File.WriteAllBytes("out.jpeg", data);
                    System.Diagnostics.Process.Start("explorer", "out.jpeg");
                    await Task.Delay(6000);
                    System.IO.File.Delete("out.jpeg");
                }
            }
            return files.Select(f => f.FileName);
        }

        // DELETE api/files/5
        [HttpDelete("{id}")]
        public void Delete(int id) {
            throw new NotImplementedException();
        }
    }
}
