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
        public async Task<List<DateTime>> Post(List<IFormFile> files, string timezone = null) {
            List<DateTime> l = new List<DateTime>();
            foreach (var file in files) {
                using (var s1 = file.OpenReadStream()) {
                    var directories = ImageMetadataReader.ReadMetadata(s1);
                    var dateTimeTag = directories.SelectMany(d => d.Tags).Where(d => d.Name == "Date/Time Original").FirstOrDefault();
                    DateTime taken = DateTime.TryParseExact(dateTimeTag?.Description, "yyyy:MM:dd HH:mm:ss", null, DateTimeStyles.AssumeLocal, out DateTime dt)
                        ? dt
                        : DateTime.UtcNow;
                    l.Add(taken);
                }
            }
            return l;
        }

        // DELETE api/files/5
        [HttpDelete("{id}")]
        public void Delete(int id) {
            throw new NotImplementedException();
        }
    }
}
