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
#warning TODO: test events
#warning TODO: test axis scaling
            var famosFile1 = FamosFile.Open("crafted_continuous.dat");

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

            // define channels
            var channels = new List<FamosFileChannel>()
            {
                new FamosFileChannel("GEN_TEMP_1") { PropertyInfo = propertyInfo1 },
                new FamosFileChannel("GEN_TEMP_2"),
                new FamosFileChannel("GEN_TEMP_3"),
                new FamosFileChannel("GEN_TEMP_4"),

                new FamosFileChannel("GEAR_TEMP_1"),
                new FamosFileChannel("GEAR_TEMP_2"),
                new FamosFileChannel("GEAR_TEMP_3"),

                new FamosFileChannel("HYD_TEMP_1"),
                new FamosFileChannel("HYD_TEMP_2"),

                new FamosFileChannel("ENV_TEMP_1")
            };

            // calibration info (for third generator component)
            var calibrationInfo = new FamosFileCalibrationInfo()
            {
                ApplyTransformation = true,
                Factor = 10,
                Offset = 7,
                Unit = "°C"
            };

            // trigger time for all components
            var triggerTime = new FamosFileTriggerTime(DateTime.Now, FamosFileTimeMode.Normal);

            // data fields
            var length = 10; /* number of samples per channel or component, respectively. */

            /* data field with equidistant time */
            var dataField1 = new FamosFileDataField(FamosFileDataFieldType.MultipleYToSingleEquidistantTime, new List<FamosFileComponent>()
            {
                /* generator */
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length) { TriggerTime = triggerTime },
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length) { TriggerTime = triggerTime },
                new FamosFileAnalogComponent(FamosFileDataType.Int32, length, calibrationInfo) { TriggerTime = triggerTime },

                new FamosFileAnalogComponent(FamosFileDataType.Float32, length) { TriggerTime = triggerTime },

                /* gearbox */
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length) { TriggerTime = triggerTime },
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length) { TriggerTime = triggerTime },
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length) { TriggerTime = triggerTime },

                /* no group */
                new FamosFileAnalogComponent(FamosFileDataType.Int16, length) { TriggerTime = triggerTime }
            });

            /* data field with monotonous increasing time */
            var dataField2 = new FamosFileDataField(FamosFileDataFieldType.MultipleYToSingleMonotonousTime, new List<FamosFileComponent>()
            {
                /* time-axis */
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length, FamosFileDataComponentType.Primary) { TriggerTime = triggerTime },

                /* hydraulic */
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length, FamosFileDataComponentType.Secondary) { TriggerTime = triggerTime },
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length, FamosFileDataComponentType.Secondary) { TriggerTime = triggerTime },
            });

            // add events to data field
            dataField1.EventInfos.Add(new FamosFileEventInfo(new List<FamosFileEvent>()
            {
                new FamosFileEvent()
                {
                    AmplificationFactor0 = 0, AmplificationFactor1 = 1,
                    AmplitudeOffset0 = 2, AmplitudeOffset1 = 3,
                    dx = 4,
                    Index = 1,
                    Length = 6,
                    Offset = 7,
                    Time = 8,
                    x0 = 9
                },
                new FamosFileEvent()
                {
                    AmplificationFactor0 = 10, AmplificationFactor1 = 11,
                    AmplitudeOffset0 = 12, AmplitudeOffset1 = 13,
                    dx = 14,
                    Index = 2,
                    Length = 16,
                    Offset = 17,
                    Time = 18,
                    x0 = 19
                }
            }));

            // add data fields to famosFile instance
            famosFile.DataFields.Add(dataField1);
            famosFile.DataFields.Add(dataField2);

            // assign channels to components (generator)
            dataField1.Components[0].Channels.Add(channels[0]);
            dataField1.Components[1].Channels.Add(channels[1]);
            dataField1.Components[2].Channels.Add(channels[2]);

            dataField1.Components[3].Channels.Add(channels[3]);

            // assign channels to components (gearbox)
            dataField1.Components[4].Channels.Add(channels[4]);
            dataField1.Components[5].Channels.Add(channels[5]);
            dataField1.Components[6].Channels.Add(channels[6]);

            // assign channels to components (hydraulic)
            dataField2.Components[1].Channels.Add(channels[7]);
            dataField2.Components[2].Channels.Add(channels[8]);

            // assign channels to components (no group)
            dataField1.Components[7].Channels.Add(channels[9]);

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

                /* gearbox */
                new FamosFileGroup("Gearbox") { Comment = "This group contains channels related to the gearbox." },

                /* hydraulic */
                new FamosFileGroup("Hydraulic")
                {
                    Comment = "This group contains channels related to the hydraulic.",
                    PropertyInfo = propertyInfo2
                }
            });

            // get group references
            var generatorGroup = famosFile.Groups[0];
            var gearboxGroup = famosFile.Groups[1];
            var hydraulicGroup = famosFile.Groups[2];

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

            generatorGroup.Channels.AddRange(channels.Skip(0).Take(4));

            // add elements to the gearbox group
            gearboxGroup.Channels.AddRange(channels.Skip(4).Take(3));

            // add elements to the hydraulic group
            hydraulicGroup.Channels.AddRange(channels.Skip(7).Take(2));

            // add elements to top level (no group)
            famosFile.Texts.Add(new FamosFileText("Random list of texts.", new List<string>() { "Text 1.", "Text 2?", "Text 3!" }));
            famosFile.Channels.Add(channels[9]);

            // save file normally (one buffer per component)
            famosFile.Save("crafted_continuous.dat", FileMode.Create, writer =>
            {
                Program.WriteFileContent(famosFile, writer, length);
            });

            // save file interlaced (a single buffer for all components, i.e. row-wise like an excel document)
            var rawData = famosFile.RawData.First(); // This raw data instance was created by the previous call to 'famosFile.Save(...)'.
            famosFile.AlignBuffers(rawData, FamosFileAlignmentMode.Interlaced);

            famosFile.Save("crafted_interlaced.dat", FileMode.Create, writer =>
            {
                Program.WriteFileContent(famosFile, writer, length);
            }, autoAlign: false);
        }

        private static void WriteFileContent(FamosFileHeader famosFile, BinaryWriter writer, int length)
        {
            var components = famosFile.DataFields.SelectMany(dataField => dataField.Components).ToList();

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
