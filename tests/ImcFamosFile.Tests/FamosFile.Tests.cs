using System.IO;
using Xunit;

namespace ImcFamosFile.Tests
{
    public class GenericTests
    {
        [Fact]
        public void CanReadTestFamosTestFile1Header() 
        {
            // Arrange
            var filePath = "./ImcTestData/BusTrip.dat";

            // Act
            using (var famosFile = FamosFile.Open(filePath))
            {
                //
            }

            // Assert
        }

        [Fact]
        public void CanReadTestFamosTestFile2Header()
        {
            // Arrange
            var filePath = "./ImcTestData/Datensatzeditor.dat";

            // Act
            using (var famosFile = FamosFile.Open(filePath))
            {
                //
            }

            // Assert
        }

        [Fact]
        public void CanReadTestFamosTestFile3Header()
        {
            // Arrange
            var filePath = "./ImcTestData/trip_Toronto.DAT";

            // Act
            using (var famosFile = FamosFile.Open(filePath))
            {
                //
            }

            // Assert
        }

        [Fact]
        public void CanReadTestHeader()
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