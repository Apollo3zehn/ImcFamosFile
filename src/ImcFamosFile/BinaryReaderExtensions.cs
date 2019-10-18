using System.IO;

namespace ImcFamosFile
{
    internal static class BinaryReaderExtensions
    {
        public static byte[] ReadBytes(this BinaryReader binaryReader, long count)
        {
            var data = new byte[count];
            var chunkCount = count / int.MaxValue;
            var remaining = (int)(count % int.MaxValue);

            for (int i = 0; i < chunkCount; i++)
            {
                var currentPosition = i * int.MaxValue;
                binaryReader.ReadBytes(int.MaxValue).CopyTo(data, currentPosition);
            }

            var position = chunkCount * int.MaxValue;
            binaryReader.ReadBytes(remaining).CopyTo(data, position);

            return data;
        }
    }
}
