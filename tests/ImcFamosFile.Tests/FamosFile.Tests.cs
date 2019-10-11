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

            // Act
            using (var famosFile = FamosFile.Open(filePath))
            {
                //
            }

            // Assert
        }

        [Fact]
        public void CanWriteHeader()
        {
            // Arrange
            var filePath = "testdata.dat";

            using (var famosFile = FamosFile.Open(filePath))
            {
                // Act
                famosFile.Save("testdata_out.dat", FileMode.Create);
            }

            // Assert
        }
    }
}