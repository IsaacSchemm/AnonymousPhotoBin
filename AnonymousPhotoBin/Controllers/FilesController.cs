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
        public async Task<object> Post(List<IFormFile> files, string timezone = null) {
            try {
                foreach (var file in files) {
                    using (var ms = new MemoryStream()) {
                        await file.OpenReadStream().CopyToAsync(ms);
                        byte[] data = ms.ToArray();
                        string f = Guid.NewGuid().ToString() + ".png";
                        System.IO.File.WriteAllBytes(f, data);
                        System.Diagnostics.Process.Start("explorer", f);
                        await Task.Delay(6000);
                        System.IO.File.Delete(f);
                    }
                }
                return new { };
            } catch (Exception e) {
                return new { error = e.Message };
            }
        }

        // DELETE api/files/5
        [HttpDelete("{id}")]
        public void Delete(int id) {
            throw new NotImplementedException();
        }
    }
}
