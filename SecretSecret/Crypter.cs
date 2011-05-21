using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Web;
using System.Security.Cryptography;

namespace SecretSecret
{
    class Crypter
    {

        public Crypter(string key) : this(Encoding.UTF8.GetBytes(key))
        {
        }

        public Crypter(byte[] key)
        {
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
            Key = sha1.ComputeHash(key).Take(16).ToArray();
        }

        public void Encrypt(Stream input, Stream output)
        {
            AesManaged aes = GetAesManaged();
            aes.GenerateIV();
            ICryptoTransform encrypter = aes.CreateEncryptor();

            output.Write(aes.IV, 0, aes.IV.Length);
            using (CryptoStream cs = new CryptoStream(output, encrypter, CryptoStreamMode.Write))
            {
                int len;
                byte[] buffer = new byte[1024];
                while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, len);
                }
            }
        }

        public void Decrypt(Stream input, Stream output)
        {
            byte[] iv = new byte[16];
            input.Read(iv, 0, iv.Length);

            AesManaged aes = GetAesManaged();
            aes.IV = iv;
            ICryptoTransform decrypter = aes.CreateDecryptor();

            using (CryptoStream cs = new CryptoStream(output, decrypter, CryptoStreamMode.Write))
            {
                int len;
                byte[] buffer = new byte[1024];
                while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, len);
                }
            }
        }

        private AesManaged GetAesManaged()
        {
            AesManaged aes = new AesManaged();
            aes.KeySize = 128;
            aes.BlockSize = 128;
            aes.Key = Key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        private byte[] Key;

    }
}
