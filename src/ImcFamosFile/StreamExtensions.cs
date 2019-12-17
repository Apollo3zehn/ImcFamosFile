using System;
using System.IO;

namespace ImcFamosFile
{
    internal static class StreamExtensions
    {
        public static void TrySeek(this Stream stream, long offset, SeekOrigin origin)
        {
            var throwException = false;

            if (origin == SeekOrigin.Begin && offset > stream.Length)
                throwException = true;
            else if (origin == SeekOrigin.Current && stream.Position + offset > stream.Length)
                throwException = true;
            else if (origin == SeekOrigin.End && offset > 0)
                throwException = true;

            if (throwException)
                throw new FormatException("Attempt to seek beyond file limits. The file seems to be corrupt.");

            stream.Seek(offset, origin);
        }
    }
}
