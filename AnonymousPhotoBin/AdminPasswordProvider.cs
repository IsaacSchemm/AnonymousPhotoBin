using System.Security.Cryptography;
using System.Text;

namespace AnonymousPhotoBin
{
    public class AdminPasswordProvider : IAdminPasswordProvider
    {
        private static readonly HashAlgorithm _hashAlgorithm = SHA256.Create();

        private static ReadOnlySpan<byte> ComputeHash(string password, ReadOnlySpan<byte> salt)
        {
            using var ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes(password));
            ms.Write(salt);
            return _hashAlgorithm.ComputeHash(ms.ToArray());
        }

        private readonly ReadOnlyMemory<byte> _salt;
        private readonly ReadOnlyMemory<byte> _hash;

        public AdminPasswordProvider(string password)
        {
            byte[] salt = new byte[16];

            var random = new Random();
            random.NextBytes(salt);

            _salt = salt;
            _hash = ComputeHash(password, salt).ToArray();
        }

        public bool IsValid(string password)
        {
            return ComputeHash(password, _salt.Span).SequenceEqual(_hash.Span);
        }
    }
}
