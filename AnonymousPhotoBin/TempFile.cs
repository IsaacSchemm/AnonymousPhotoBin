using System.IO;
using System.Threading.Tasks;

namespace AnonymousPhotoBin {
    public class TempFile {
        private static string TempDir = Path.GetTempPath();

        public readonly string FilePath;
        public readonly string OriginalFilename;

        private TempFile(string filePath, string originalFilename) {
            this.FilePath = filePath;
            this.OriginalFilename = originalFilename;
        }

        public static async Task<TempFile> Store(Stream s, string originalFilename) {
            string file = Path.GetTempFileName();
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Write)) {
                await s.CopyToAsync(fs);
            }
            return new TempFile(file, originalFilename);
        }

        public void Delete() {
            File.Delete(FilePath);
        }
    }
}
