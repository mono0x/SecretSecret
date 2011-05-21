using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace SecretSecret
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 3) {
                Console.Error.WriteLine("Usage: SecretSecret key data-dir dropbox-dir");
                return;
            }

            var key = args[0];
            var local = args[1];
            var remote = args[2];

            Crypter crypter = new Crypter(key);

            Synchronizer synchronizer = new Synchronizer(crypter, local, remote);

            for (; ; )
            {
                synchronizer.Update();
                Thread.Sleep(5000);
            }
        }
    }
}
