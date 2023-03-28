using ImcFamosFile;
using System.Text;

namespace ImcFamosFileSample
{
    public class Program
    {
        public static double[] PowerData => new double[] { 0, 0, 55, 175, 410, 760, 1250, 1900, 2700, 3750, 4850, 5750, 6500, 7000, 7350, 7500, 7580, 7580, 7580, 7580, 7580, 7580, 7580, 7580, 7580 };
        public static double[] PowerCoeffData => new double[] { 0.00, 0.000, 0.263, 0.352, 0.423, 0.453, 0.470, 0.478, 0.477, 0.483, 0.470, 0.429, 0.381, 0.329, 0.281, 0.236, 0.199, 0.168, 0.142, 0.122, 0.105, 0.092, 0.080, 0.071, 0.063 };
        public static double[] WindSpeedData => Enumerable.Range(1, 25).Select(value => (double)value).ToArray();

        static void Main()
        {
            var afamosFile = FamosFile.Open(@"Q:\RAW\DB_ACHTERMEER_V66\calibrated\2014-11\2014-11-19\2014-11-19_00-00-00.dat");

            var famosFile = new FamosFileHeader();

            Program.PrepareHeader(famosFile);

            // OPTION 1: Save file normally (one buffer per component).

                /* Save file. */
                famosFile.Save("continuous.dat", FileMode.Create, writer => Program.WriteFileContent(famosFile, writer));

                /* Remove -automatically- added raw block (only required to get correct initial conditions for OPTION 2). */
                famosFile.RawBlocks.Clear();

            // OPTION 2: Save file interlaced (a single buffer for all components, i.e. write data row-wise like in an Excel document).

                /* A raw block must be added manually when option 'autoAlign: false'. is set. */
                famosFile.RawBlocks.Add(new FamosFileRawBlock());

                /* Align buffers (lengths and offsets) to get an interlaced layout.*/
                famosFile.AlignBuffers(famosFile.RawBlocks.First(), FamosFileAlignmentMode.Interlaced);

                /* Save file with option 'autoAlign: false'. */
                famosFile.Save("interlaced.dat", FileMode.Create, writer => Program.WriteFileContent(famosFile, writer), autoAlign: false);
        }

        public static void PrepareHeader(FamosFileHeader famosFile)
        {
            var encoding = Encoding.GetEncoding(1252);

            // add language info
            famosFile.LanguageInfo = new FamosFileLanguageInfo() { CodePage = encoding.CodePage };

            // add data origin info
            famosFile.OriginInfo = new FamosFileOriginInfo("ImcFamosFile", FamosFileOrigin.Calculated);

            // add custom key
            var customKey = new FamosFileCustomKey("FileID", encoding.GetBytes(Guid.NewGuid().ToString()));
            famosFile.CustomKeys.Add(customKey);

            // data fields
            var length = 25; /* number of samples per channel or component, respectively. */

            /* data field with equidistant time */
            var calibrationInfo1 = new FamosFileCalibration()
            {
                ApplyTransformation = true,
                Factor = 10,
                Offset = 7,
                Unit = "°C"
            };

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
                XAxisScaling = new FamosFileXAxisScaling(deltaX: 0.01M) { X0 = 985.0M, Unit = "Seconds" }
            });

            famosFile.Fields[0].Components[0].Channels[0].PropertyInfo = new FamosFilePropertyInfo(new List<FamosFileProperty>()
            {
                new FamosFileProperty("Sensor Location", "Below generator.")
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

            /* data field for characteristic curves */
            var calibrationInfo2 = new FamosFileCalibration() { Unit = "kW" };
            var calibrationInfo3 = new FamosFileCalibration() { Unit = "-" };
            var calibrationInfo4 = new FamosFileCalibration() { Unit = "m/s" };

            famosFile.Fields.Add(new FamosFileField(FamosFileFieldType.MultipleYToSingleXOrViceVersa, new List<FamosFileComponent>()
            {
                /* power */
                new FamosFileAnalogComponent("POWER", FamosFileDataType.Float64, length, calibrationInfo2),

                /* power coefficient */
                new FamosFileAnalogComponent("POWER_COEFF", FamosFileDataType.Float64, length, calibrationInfo3),

                /* wind speed */
                new FamosFileAnalogComponent(FamosFileDataType.Float64, length, FamosFileComponentType.Secondary, calibrationInfo4),
            }));

            /* data field with complex values */
            var calibrationInfo5 = new FamosFileCalibration() { Unit = "A" };

            famosFile.Fields.Add(new FamosFileField(FamosFileFieldType.ComplexRealImaginary, new List<FamosFileComponent>()
            {
                /* converter data (real part) */
                new FamosFileAnalogComponent("CONV_CURRENT_L1", FamosFileDataType.Float32, length, FamosFileComponentType.Primary, calibrationInfo5),

                /* converter data (imaginary part) */
                new FamosFileAnalogComponent(FamosFileDataType.Float32, length, FamosFileComponentType.Secondary, calibrationInfo5),
            })
            {
                XAxisScaling = new FamosFileXAxisScaling(deltaX: 1 / 25000M)
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
                new FamosFileProperty("Weight", 3752.23),
                new FamosFileProperty("Start-up date", new DateTime(2019, 12, 06, 11, 41, 30, 210))
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

                /* power curve */
                new FamosFileGroup("Power curve"),

                /* converter */
                new FamosFileGroup("Converter") { Comment = "This group contains channels related to the converter." }
            });

            // get group references
            var generatorGroup = famosFile.Groups[0];
            var hydraulicGroup = famosFile.Groups[1];
            var powerCurveGroup = famosFile.Groups[2];
            var converterGroup = famosFile.Groups[3];

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
                    new FamosFileProperty("Length", 318)
                })
            });

            generatorGroup.Channels.AddRange(famosFile.Fields[0].GetChannels().Take(4));

            // add elements to the hydraulic group
            hydraulicGroup.Channels.AddRange(famosFile.Fields[1].GetChannels());

            // add elements to the power curve group
            powerCurveGroup.Texts.Add(new FamosFileText("Type", "E-126"));
            powerCurveGroup.Texts.Add(new FamosFileText("Source", "Enercon Product Sheet (2015)"));

            powerCurveGroup.Channels.AddRange(famosFile.Fields[2].GetChannels());

            // add elements to the converter group
            converterGroup.Channels.AddRange(famosFile.Fields[3].GetChannels());

            // add other elements to top level (no group)
            famosFile.Texts.Add(new FamosFileText("Random list of texts.", new List<string>() { "Text 1.", "Text 2?", "Text 3!" }));
            famosFile.Channels.Add(famosFile.Fields[0].GetChannels().Last());
        }

        public static void WriteFileContent(FamosFileHeader famosFile, BinaryWriter writer)
        {
            var length = 25;
            var components = famosFile.Fields.SelectMany(field => field.Components).ToList();

            // Generate some 'random' datasets and write them to file. One dataset per component.
            for (int i = 0; i < components.Count; i++)
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

            // add real data to power curve components
            var powerComponent = famosFile.Fields[2].Components[0];
            famosFile.WriteSingle(writer, powerComponent, Program.PowerData);

            var powerCoeffComponent = famosFile.Fields[2].Components[1];
            famosFile.WriteSingle(writer, powerCoeffComponent, Program.PowerCoeffData);

            var windSpeedComponent = famosFile.Fields[2].Components[2];
            famosFile.WriteSingle(writer, windSpeedComponent, Program.WindSpeedData);
        }
    }
}
