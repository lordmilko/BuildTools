using System;
using System.IO;
using System.Security.Cryptography;

namespace BuildTools
{
    interface IHasher
    {
        string HashFile(string fileName);
    }

    class Hasher : IHasher
    {
        public string HashFile(string fileName)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(fileName))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}