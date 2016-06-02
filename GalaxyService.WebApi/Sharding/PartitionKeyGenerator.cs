using System;
using System.Security.Cryptography;
using System.Text;

namespace GalaxyService.WebApi.Sharding
{
    public static class PartitionKeyGenerator
    {
        public static long Generate(string value)
        {
            byte[] byteContents = Encoding.Unicode.GetBytes(value);
            MD5CryptoServiceProvider hash = new MD5CryptoServiceProvider();
            byte[] hashText = hash.ComputeHash(byteContents);
            return BitConverter.ToInt64(hashText, 0) ^ BitConverter.ToInt64(hashText, 7);
        }
    }
}
