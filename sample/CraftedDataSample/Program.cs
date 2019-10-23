using ImcFamosFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FamosFileSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // famos file
            var famosFile = new FamosFileHeader();
            var encoding = Encoding.GetEncoding(1252);

            // add language info
            famosFile.LanguageInfo = new FamosFileLanguageInfo() { CodePage = encoding.CodePage };

            // add data origin info
            famosFile.DataOriginInfo = new FamosFileDataOriginInfo("ImcFamosFile", FamosFileDataOrigin.Calculated);

            // add custom key
            famosFile.CustomKeys.Add(new FamosFileCustomKey("FileID", encoding.GetBytes(Guid.NewGuid().ToString())));

            // property info (for first generator channel)
            var propertyInfo1 = new FamosFilePropertyInfo(new List<FamosFileProperty>()
            {
                new FamosFileProperty("Sensor Location", "Below generator.", FamosFilePropertyType.String)
            });

            // data fields
            var length = 10; /* number of samples per channel or component, respectively. */

            /* calibration info (for third generator component) */
            var calibrationInfo1 = new FamosFileCalibrationInfo()
            {
                ApplyTransformation = true,
                Factor = 10,
                Offset = 7,
                Unit = "°C"
            };

            var calibrationInfo2 = new FamosFileCalibrationInfo()
            {
                Unit = "A"
            };

            /* data field with equidistant time */
            famosFile.Fields.Add(new FamosFileField(FamosFileFieldType.MultipleYToSingleEquidistantTime, new List<FamosFileComponent>()
            {
                /* generator data */
                new FamosFileAnalogComponent("GEN_TEMP_1", FamosFileDataType.Float32, length, calibrationInfo1),
                new FamosFileAnalogComponent("GEN_TEMP_2", FamosFileDataType.Float32, length, calibrationInfo1),
                new FamosFileAnalogComponent("GEN_TEMP_3", FamosFileDataType.Int32, length, calibrationInfo1),

                new FamosFileAnalogComponent("GEN_TEMP_4", FamosFileDataType.Float32, length, calibrationInfo1),

                /* no group */
                new FamosFileAnalogComponent("ENV_TEMP_1", FamosFileDataType.Int16, length, calibrationInfo1)
            })
            {
                TriggerTime = new FamosFileTriggerTime(DateTime.Now, FamosFileTimeMode.Normal),

                XAxisScaling = new FamosFileXAxisScaling(deltaX: 985.0M)
                {
                    DeltaX = 0.01M,
                    Unit = "Seconds"
                }
            });

            /* data field with monotonous increasing time */
            famosFile.Fields.Add(new FamosFileField(FamosFileFieldType.MultipleYToSingleMonotonousTime, new List<FamosFileComponent>()
            {
                /* hydraulic data */
                new FamosFileAnalogComponent("HYD_TEMP_1", FamosFileDataType.Float32, length, calibrationInfo1),
                new FamosFileAnalogComponent("HYD_TEMP_2", FamosFileDataType.Float32, length, calibrationInfo1),

                /* time-axis */
                new FamosFileAnalogComponent(FamosFileDataType.UInt32, length, FamosFileComponentType.Secondary),
            })
            {
                XAxisScaling = new FamosFileXAxisScaling(deltaX: 100M)
                {
                    X0 = 0M,
                    Unit = "Milliseconds"
                }
            });

            /* data field with complex values */
            famosFile.Fields.Add(new FamosFileField(FamosFileFieldType.ComplexRealImaginary, new List<FamosFileComponent>()
            {
                /* converter data (real part) */
                new FamosFileAnalogComponent("CONV_CURRENT_L1", FamosFileDataType.Float32, length, FamosFileComponentType.Primary, calibrationInfo2),

                /* converter data (imaginary part) */
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length, FamosFileComponentType.Secondary, calibrationInfo2),
            })
            {
                XAxisScaling = new FamosFileXAxisScaling(deltaX: 1/25000M)
                {
                    X0 = 0M,
                    Unit = "Seconds"
                },
                ZAxisScaling = new FamosFileZAxisScaling(deltaZ: 5M)
                {
                    Z0 = 0M,
                    SegmentSize = 2,
                    Unit = "Meters"
                }
            });

            // add events to data field (not supported yet)

            //field1.EventInfos.Add(new FamosFileEventInfo(new List<FamosFileEvent>()
            //{
            //    new FamosFileEvent()
            //    {
            //        AmplificationFactor0 = 0, AmplificationFactor1 = 1,
            //        AmplitudeOffset0 = 2, AmplitudeOffset1 = 3,
            //        deltaX = 4,
            //        Index = 1,
            //        Length = 6,
            //        Offset = 7,
            //        Time = 8,
            //        x0 = 9
            //    },
            //    new FamosFileEvent()
            //    {
            //        AmplificationFactor0 = 10, AmplificationFactor1 = 11,
            //        AmplitudeOffset0 = 12, AmplitudeOffset1 = 13,
            //        deltaX = 14,
            //        Index = 2,
            //        Length = 16,
            //        Offset = 17,
            //        Time = 18,
            //        x0 = 19
            //    }
            //}));

            // property info (for hydraulic group)
            var propertyInfo2 = new FamosFilePropertyInfo(new List<FamosFileProperty>()
            {
                new FamosFileProperty("Weight", "3752.23", FamosFilePropertyType.Real)
            });

            // define groups
            famosFile.Groups.AddRange(new List<FamosFileGroup>()
            {
                /* generator */
                new FamosFileGroup("Generator") { Comment = "This group contains channels related to the generator." },

                /* hydraulic */
                new FamosFileGroup("Hydraulic")
                {
                    Comment = "This group contains channels related to the hydraulic unit.",
                    PropertyInfo = propertyInfo2
                },

                /* converter */
                new FamosFileGroup("Converter") { Comment = "This group contains channels related to the converter." }
            });

            // get group references
            var generatorGroup = famosFile.Groups[0];
            var hydraulicGroup = famosFile.Groups[1];
            var converterGroup = famosFile.Groups[2];

            // add elements to the generator group
            generatorGroup.SingleValues.Add(new FamosFileSingleValue<double>("GEN_TEMP_1_AVG", 40.25)
            {
                Comment = "Generator temperature 1.",
                Unit = "°C",
                Time = DateTime.Now,
            });

            generatorGroup.Texts.Add(new FamosFileText("Description", "In electricity generation, a generator is a device that converts motive power (mechanical energy) into electrical power for use in an external circuit. Sources of mechanical energy include steam turbines, gas turbines, water turbines, internal combustion engines, wind turbines and even hand cranks. - Wikipedia (2019)")
            {
                Comment = "Maybe its useful.",
                PropertyInfo = new FamosFilePropertyInfo(new List<FamosFileProperty>()
                {
                    new FamosFileProperty("Length", "318", FamosFilePropertyType.Integer)
                })
            });

            generatorGroup.Channels.AddRange(famosFile.Fields[0].GetChannels().Take(4));

            // add elements to the hydraulic group
            hydraulicGroup.Channels.AddRange(famosFile.Fields[1].GetChannels());

            // add elements to the converter group
            converterGroup.Channels.AddRange(famosFile.Fields[2].GetChannels());

            // add other elements to top level (no group)
            famosFile.Texts.Add(new FamosFileText("Random list of texts.", new List<string>() { "Text 1.", "Text 2?", "Text 3!" }));
            famosFile.Channels.Add(famosFile.Fields[0].GetChannels().Last());

            // OPTION 1: save file normally (one buffer per component)
            famosFile.Save("crafted_continuous.dat", FileMode.Create, writer => Program.WriteFileContent(famosFile, writer, length));

            // OPTION 2: save file interlaced (a single buffer for all components, i.e. write data row-wise like in an Excel document)
            var rawData = famosFile.RawData.First(); // This raw data instance was created by the previous call to 'famosFile.Save(...)'.

            famosFile.AlignBuffers(rawData, FamosFileAlignmentMode.Interlaced);
            famosFile.Save("crafted_interlaced.dat", FileMode.Create, writer => Program.WriteFileContent(famosFile, writer, length), autoAlign: false);
        }

        private static void WriteFileContent(FamosFileHeader famosFile, BinaryWriter writer, int length)
        {
            var components = famosFile.Fields.SelectMany(field => field.Components).ToList();

            // Generate some 'random' datasets and write them to file. One dataset per component.
            for (int i = 0; i < components.Count(); i++)
            {
                var component = components[i];

                switch (component.PackInfo.DataType)
                {
                    case FamosFileDataType.Int16:
                        var shortData = Enumerable.Range(0, length).Select(value => (short)(value + i * 100)).ToArray();
                        famosFile.WriteSingle(writer, component, shortData);
                        break;

                    case FamosFileDataType.Int32:
                    case FamosFileDataType.UInt32:
                        var intData = Enumerable.Range(0, length).Select(value => value + i * 100).ToArray();
                        famosFile.WriteSingle(writer, component, intData);
                        break;

                    case FamosFileDataType.Float32:
                        var floatData = Enumerable.Range(0, length).Select(value => (float)(value + i * 100 + 0.1)).ToArray();
                        famosFile.WriteSingle(writer, component, floatData);
                        break;

                    default:
                        continue;
                }
            }
        }
    }
}
