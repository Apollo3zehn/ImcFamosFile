using System.IO;
using Xunit;

namespace ImcFamosFile.Tests
{
    public class GenericTests
    {
        [Fact]
        public void CanReadHeader() 
        {
            // Arrange
            var filePath = "testdata.dat";

            using (var reader = new BinaryReader(File.OpenRead(filePath)))
            {
                // Act
                var famosFile = new FamosFile(reader);

                // Assert
            }
        }
    }
}