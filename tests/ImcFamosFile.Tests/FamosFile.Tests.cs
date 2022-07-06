using System;
using System.Collections.Generic;
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

#warning This errors on Linux. Is this a bug or a "feature" of the Famos specs?
            //var unit = ((FamosFileAnalogComponent)famosFileRead.Fields[0].Components[0]).CalibrationInfo.Unit;
            //Assert.Equal("°C", unit);

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
        public void CanEditAlreadyExistingFile()
        {
            // Arrange
            var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var famosFileHeader = new FamosFileHeader();
            var components = new List<FamosFileComponent>() { new FamosFileAnalogComponent("C1", FamosFileDataType.Float64, 5) };

            famosFileHeader.Fields.Add(new FamosFileField(FamosFileFieldType.MultipleYToSingleEquidistantTime, components));
            famosFileHeader.Channels.Add(components.First().Channels.First());
            famosFileHeader.Save(filePath, writer => famosFileHeader.WriteSingle(writer, components.First(), new double[] { 0, 0, 0, 0, 0 }));

            // Act
            /* write data */
            var expected = new double[] { 1, 2, 3, 4, 5 };

            using (var famosFile = FamosFile.OpenEditable(filePath))
            {
                var component = famosFile.Fields.First().Components.First();
                famosFile.Edit(writer => famosFile.WriteSingle(writer, component, expected));
            }

            /* read data */
            using var famosFile2 = FamosFile.Open(filePath);

            var channelData = famosFile2.ReadSingle(famosFile2.Channels.First());
            var actual = ((FamosFileComponentData<double>)channelData.ComponentsData.First()).Data.ToArray();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DistributesPropertiesForEachComponent()
        {
            // Arrange
            var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var famosFileHeader = new FamosFileHeader();

            var components = new List<FamosFileComponent>() 
            {
                new FamosFileAnalogComponent("A", FamosFileDataType.Float64, 5),
                new FamosFileAnalogComponent("B", FamosFileDataType.UInt16, 15),
                new FamosFileAnalogComponent("C", FamosFileDataType.Int32, 7),
            };

            components[1].XAxisScaling = new FamosFileXAxisScaling(1);
            components[2].XAxisScaling = components[1].XAxisScaling;

            famosFileHeader.Fields.Add(new FamosFileField(FamosFileFieldType.MultipleYToSingleEquidistantTime, components));
            famosFileHeader.Channels.AddRange(components.SelectMany(component => component.Channels));

            // Act
            famosFileHeader.Save(filePath, writer => { });

            using var famosFile = FamosFile.Open(filePath);

            // Assert
            DoAssert(famosFileHeader.Fields[0]);
            DoAssert(famosFile.Fields[0]);

            void DoAssert(FamosFileField field)
            {
                Assert.True(field.XAxisScaling == null);
                Assert.True(field.Components[0].XAxisScaling == null);
                Assert.True(field.Components[1].XAxisScaling != null);
                Assert.True(field.Components[2].XAxisScaling != null);
                Assert.True(!(field.Components[1].XAxisScaling == field.Components[2].XAxisScaling));
            }
        }

        [Fact]
        public void ThrowsWhenReadingCorruptFile()
        {
            // Arrange
            var filePath = $"./ImcTestData/BusTrip_corrupt.dat";

            // Act
            Action action = () => FamosFile.Open(filePath);

            // Assert
            Assert.Throws<FormatException>(action);
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
            Action action = () => famosFile.Validate();

            // Assert
            Assert.Throws<FormatException>(action);
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
            Action action = () => famosFile.Validate();

            // Assert
            Assert.Throws<FormatException>(action);
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
            Action action = () => famosFile.Validate();

            // Assert
            Assert.Throws<FormatException>(action);
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
            Action action = () => famosFile.Validate();

            // Assert
            Assert.Throws<FormatException>(action);
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
            Action action = () => famosFile.Validate();

            // Assert
            Assert.Throws<FormatException>(action);
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
            Action action = () => famosFile.Validate();

            // Assert
            Assert.Throws<FormatException>(action);
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
            Action action = () => famosFile.Validate();

            // Assert
            Assert.Throws<FormatException>(action);
        }
    }
}