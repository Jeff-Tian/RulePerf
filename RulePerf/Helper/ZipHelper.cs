using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Helper
{
    public class ZipHelper
    {
        public const int BUFFER_SIZE = 4096;

        public static void CompressFile(string sourceFileFullName, string destFileFullName)
        {
            using (FileStream source = File.OpenRead(sourceFileFullName))
            using(FileStream dest = File.Create(destFileFullName))
            {
                byte[] buffer = new byte[source.Length];
                source.Read(buffer, 0, buffer.Length);
                using (GZipStream output = new GZipStream(dest, CompressionMode.Compress, false))
                {
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
                dest.Close();
                source.Close();
            }
        }

        public static string CompressFile(string sourceFileFullName)
        {
            string destFileFullName = "{0}.gz".FormatWith(sourceFileFullName);
            CompressFile(sourceFileFullName, destFileFullName);
            return destFileFullName;
        }

        public static void DecompressFile(string sourceFileFullName, string destFileFullName)
        {
            using(FileStream source = File.OpenRead(sourceFileFullName))
            using (FileStream dest = File.Create(destFileFullName))
            {
                using (GZipStream input = new GZipStream(source, CompressionMode.Decompress, false))
                {
                    // Because the uncompressed size of the file is unknown, 
                    // we are using an arbitrary buffer size.
                    byte[] buffer = new byte[BUFFER_SIZE];
                    int n;
                    while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                        dest.Write(buffer, 0, n);

                    input.Close();
                }
                dest.Close();
                source.Close();
            }
        }

        public static string DecompressFile(string sourceFileFullName)
        {
            string destPath = Path.Combine(Path.GetDirectoryName(sourceFileFullName), Path.GetFileNameWithoutExtension(sourceFileFullName));
            DecompressFile(sourceFileFullName, destPath);

            return destPath;
        }
    }
}
