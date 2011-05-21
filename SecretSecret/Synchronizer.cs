using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;

namespace SecretSecret
{
    class Synchronizer
    {
        public Synchronizer(Crypter crypter, string left, string right)
        {
            Crypter = crypter;
            Left = left;
            Right = right;

            UpdateInternalStates();
        }

        public void Update()
        {
            SynchronizeDirectory(Left, Right);
            UpdateInternalStates();
        }

        private void UpdateInternalStates()
        {
            PreviousLeftDirectories =
                new HashSet<string>(Directory.GetDirectories(Left, "*", SearchOption.AllDirectories));
            PreviousRightDirectories =
                new HashSet<string>(Directory.GetDirectories(Right, "*", SearchOption.AllDirectories));
            PreviousLeftFiles =
                new HashSet<string>(Directory.GetFiles(Left, "*", SearchOption.AllDirectories));
            PreviousRightFiles =
                new HashSet<string>(Directory.GetFiles(Right, "*", SearchOption.AllDirectories));
        }

        private void SynchronizeDirectory(string leftRoot, string rightRoot)
        {
            SynchronizeChildren(leftRoot, rightRoot);
            SynchronizeFiles(leftRoot, rightRoot);
        }

        private void SynchronizeFiles(string leftRoot, string rightRoot)
        {
            var leftFiles =
                new HashSet<string>(Directory.GetFiles(leftRoot).Select(p => RelativePath(Left, p)));
            var rightFiles =
                new HashSet<string>(Directory.GetFiles(rightRoot).Select(p => RelativePath(Right, p)));
            var files = new HashSet<string>(leftFiles.Union(rightFiles));

            foreach (var file in files)
            {
                var left = Path.Combine(Left, file);
                var right = Path.Combine(Right, file);
                var leftExists = File.Exists(left);
                var rightExists = File.Exists(right);
                if (leftExists && rightExists)
                {
                    var diff = (File.GetLastWriteTime(left) - File.GetLastWriteTime(right)).Ticks;
                    if (diff > 0)
                    {
                        CopyWithEncrypt(left, right);
                    }
                    else if (diff < 0)
                    {
                        CopyWithDecrypt(right, left);
                    }
                }
                else if (leftExists && !rightExists)
                {
                    if (PreviousRightFiles.Contains(right))
                    {
                        File.Delete(left);
                    }
                    else
                    {
                        CopyWithEncrypt(left, right);
                    }
                }
                else if (!leftExists && rightExists)
                {
                    if (PreviousLeftFiles.Contains(left))
                    {
                        File.Delete(right);
                    }
                    else
                    {
                        CopyWithDecrypt(right, left);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void SynchronizeChildren(string leftRoot, string rightRoot)
        {
            var leftDirectories =
                new HashSet<string>(Directory.GetDirectories(leftRoot).Select(p => RelativePath(Left, p)));
            var rightDirectories =
                new HashSet<string>(Directory.GetDirectories(rightRoot).Select(p => RelativePath(Right, p)));
            var directories = new HashSet<string>(leftDirectories.Union(rightDirectories));

            foreach (var directory in directories)
            {
                var left = Path.Combine(Left, directory);
                var right = Path.Combine(Right, directory);
                var leftExists = Directory.Exists(left);
                var rightExists = Directory.Exists(right);
                if (leftExists && rightExists)
                {
                    SynchronizeDirectory(left, right);
                }
                else if (leftExists && !rightExists)
                {
                    if (PreviousRightDirectories.Contains(right))
                    {
                        Directory.Delete(left, true);
                    }
                    else
                    {
                        Directory.CreateDirectory(right);
                        SynchronizeDirectory(left, right);
                    }
                }
                else if (!leftExists && rightExists)
                {
                    if (PreviousLeftDirectories.Contains(left))
                    {
                        Directory.Delete(right, true);
                    }
                    else
                    {
                        Directory.CreateDirectory(left);
                        SynchronizeDirectory(left, right);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static string RelativePath(string root, string path)
        {
            return HttpUtility.UrlDecode(new Uri(root).MakeRelativeUri(new Uri(path)).ToString());
        }

        private bool CopyWithEncrypt(string source, string destination)
        {
            try
            {
                using (FileStream sourceStream = new FileStream(source, FileMode.Open))
                {
                    using (FileStream destinationStream = new FileStream(destination, FileMode.OpenOrCreate))
                    {
                        Crypter.Encrypt(sourceStream, destinationStream);
                    }
                }
                File.SetLastWriteTime(destination, File.GetLastWriteTime(source));
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }

        private bool CopyWithDecrypt(string source, string destination)
        {
            try
            {
                using (FileStream sourceStream = new FileStream(source, FileMode.Open))
                {
                    using (FileStream destinationStream = new FileStream(destination, FileMode.OpenOrCreate))
                    {
                        Crypter.Decrypt(sourceStream, destinationStream);
                    }
                }
                File.SetLastWriteTime(destination, File.GetLastWriteTime(source));
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }

        private Crypter Crypter;

        private string Left;

        private string Right;

        private HashSet<string> PreviousLeftDirectories;

        private HashSet<string> PreviousRightDirectories;

        private HashSet<string> PreviousLeftFiles;

        private HashSet<string> PreviousRightFiles;

    }
}
