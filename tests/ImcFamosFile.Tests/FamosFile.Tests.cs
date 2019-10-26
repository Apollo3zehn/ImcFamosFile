using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ImcFamosFile.Tests
{
    public class FamosFileTests
    {
        [Theory]
        [InlineData("BusTrip.dat")]
        [InlineData("Datensatzeditor.dat")]
        [InlineData("trip_Toronto.DAT")]
        public void CanReadTestImcTestData(string fileName)
        {
            // Arrange
            var filePath = $"./ImcTestData/{fileName}";

            // Act
            using (var famosFile = FamosFile.Open(filePath))
            {
                //
            }

            // Assert
        }

        [Theory]
        [InlineData("continuous")]
        [InlineData("interlaced")]
        public void CanReadWriteSampleData(string type)
        {
            // Arrange
            var stream = new MemoryStream();
            var famosFileWrite = new FamosFileHeader();

            ImcFamosFileSample.Program.PrepareHeader(famosFileWrite);

            if (type == "interlaced")
            {
                famosFileWrite.RawBlocks.Add(new FamosFileRawBlock());
                famosFileWrite.AlignBuffers(famosFileWrite.RawBlocks.First(), FamosFileAlignmentMode.Interlaced);
                famosFileWrite.Save(stream, writer => ImcFamosFileSample.Program.WriteFileContent(famosFileWrite, writer), autoAlign: false);
            }
            else
            {
                famosFileWrite.Save(stream, writer => ImcFamosFileSample.Program.WriteFileContent(famosFileWrite, writer));
            }

            // Act
            using var famosFileRead = FamosFile.Open(stream);
            var allData = famosFileRead.ReadAll();
            var singleData = famosFileRead.ReadSingle(famosFileRead.Fields[2].Components[0].Channels.First());
            var singleDataPartial = famosFileRead.ReadSingle(famosFileRead.Fields[2].Components[0].Channels.First(), 10, 8);

            // Assert

            // all data
            var expectedPower1 = ImcFamosFileSample.Program.PowerData;
            var actualPower1 = ((FamosFileComponentData<double>)allData[7].ComponentsData[0]).Data.ToArray();
            Assert.Equal(expectedPower1, actualPower1);

            var expectedPowerCoeff = ImcFamosFileSample.Program.PowerCoeffData;
            var actualPowerCoeff = ((FamosFileComponentData<double>)allData[8].ComponentsData[0]).Data.ToArray();
            Assert.Equal(expectedPowerCoeff, actualPowerCoeff);

            var expectedWindSpeed = ImcFamosFileSample.Program.WindSpeedData;
            var actualWindSpeed = ((FamosFileComponentData<double>)allData[7].ComponentsData[1]).Data.ToArray();
            Assert.Equal(expectedWindSpeed, actualWindSpeed);

            // singleData
            var expectedPower2 = ImcFamosFileSample.Program.PowerData;
            var actualPower2 = ((FamosFileComponentData<double>)singleData.ComponentsData[0]).Data.ToArray();
            Assert.Equal(expectedPower2, actualPower1);

            // singleDataPartial
            var expectedPower3 = ImcFamosFileSample.Program.PowerData.Skip(10).Take(8);
            var actualPower3 = ((FamosFileComponentData<double>)singleDataPartial.ComponentsData[0]).Data.ToArray();
            Assert.Equal(expectedPower3, actualPower3);
        }

        [Fact]
        public void ThrowsWhenGroupIsAddedTwice()
        {
            // Arrange
            var famosFile = new FamosFileHeader();
            var group = new FamosFileGroup("Group 1");

            famosFile.Groups.Add(group);
            famosFile.Groups.Add(group);

            // Act
            Assert.Throws<FormatException>(() => famosFile.Validate());
        }

        [Fact]
        public void ThrowsWhenCustomKeyIsAddedTwice()
        {
            // Arrange
            var famosFile = new FamosFileHeader();
            var customKey = new FamosFileCustomKey("Custom key 1", Encoding.ASCII.GetBytes("Value 1"));

            famosFile.CustomKeys.Add(customKey);
            famosFile.CustomKeys.Add(customKey);

            // Act
            Assert.Throws<FormatException>(() => famosFile.Validate());
        }

        [Fact]
        public void ThrowsWhenFieldIsAddedTwice()
        {
            // Arrange
            var famosFile = new FamosFileHeader();
            var field = new FamosFileField();

            famosFile.Fields.Add(field);
            famosFile.Fields.Add(field);

            // Act
            Assert.Throws<FormatException>(() => famosFile.Validate());
        }

        [Fact]
        public void ThrowsWhenRawBlockIsAddedTwice()
        {
            // Arrange
            var famosFile = new FamosFileHeader();
            var rawBlock = new FamosFileRawBlock();

            famosFile.RawBlocks.Add(rawBlock);
            famosFile.RawBlocks.Add(rawBlock);

            // Act
            Assert.Throws<FormatException>(() => famosFile.Validate());
        }

        [Fact]
        public void ThrowsWhenSingleValueIsAddedTwice()
        {
            // Arrange
            var famosFile = new FamosFileHeader();
            var group = new FamosFileGroup("Group 1");
            var singleValue = new FamosFileSingleValue<float>("Single Value 1", 1);

            group.SingleValues.Add(singleValue);
            famosFile.SingleValues.Add(singleValue);
            famosFile.Groups.Add(group);

            // Act
            Assert.Throws<FormatException>(() => famosFile.Validate());
        }

        [Fact]
        public void ThrowsWhenTextIsAddedTwice()
        {
            // Arrange
            var famosFile = new FamosFileHeader();
            var group = new FamosFileGroup("Group 1");
            var text = new FamosFileText("Text 1", "Value 1");

            group.Texts.Add(text);
            famosFile.Texts.Add(text);
            famosFile.Groups.Add(group);

            // Act
            Assert.Throws<FormatException>(() => famosFile.Validate());
        }

        [Fact]
        public void ThrowsWhenChannelIsAddedTwice()
        {
            // Arrange
            var famosFile = new FamosFileHeader();
            var group = new FamosFileGroup("Group 1");
            var channel = new FamosFileChannel("Channel 1");

            group.Channels.Add(channel);
            famosFile.Channels.Add(channel);
            famosFile.Groups.Add(group);

            // Act
            Assert.Throws<FormatException>(() => famosFile.Validate());
        }
    }
}