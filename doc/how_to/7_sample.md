Below is a short sample extracted from the full sample, which you can find [here](https://github.com/Apollo3zehn/ImcFamosFile/blob/master/sample/ImcFamosFileSample/Program.cs).

```cs
var famosFile = new FamosFileHeader();

famosFile.Fields.Add(new FamosFileField(FamosFileFieldType.MultipleYToSingleEquidistantTime, new List<FamosFileComponent>()
{
    var length = 25; /* number of samples per channel */
    var calibrationInfo = new FamosFileCalibration() { Unit = "°C" };

    /* generator data */
    new FamosFileAnalogComponent("GEN_TEMP_1", FamosFileDataType.Float32, length, calibrationInfo),
    new FamosFileAnalogComponent("GEN_TEMP_2", FamosFileDataType.Float32, length, calibrationInfo),
    new FamosFileAnalogComponent("GEN_TEMP_3", FamosFileDataType.Int32, length, calibrationInfo),
    new FamosFileAnalogComponent("GEN_TEMP_4", FamosFileDataType.Float32, length, calibrationInfo),

    /* no group */
    new FamosFileAnalogComponent("ENV_TEMP_1", FamosFileDataType.Int16, length, calibrationInfo)
})
{
    TriggerTime = new FamosFileTriggerTime(DateTime.Now, FamosFileTimeMode.Normal),
    XAxisScaling = new FamosFileXAxisScaling(deltaX: 0.01M) { X0 = 985.0M, Unit = "Seconds" }
});

// define groups
famosFile.Groups.AddRange(new List<FamosFileGroup>()
{
    /* generator */
    new FamosFileGroup("Generator") { Comment = "This group contains channels related to the generator." },

    /* hydraulic */
    new FamosFileGroup("Hydraulic");
});

// get generator group reference
var generatorGroup = famosFile.Groups[0];

// add elements to the generator group
generatorGroup.SingleValues.Add(new FamosFileSingleValue<double>("GEN_TEMP_1_AVG", 40.25)
{
    Comment = "Generator temperature 1.",
    Unit = "°C",
    Time = DateTime.Now,
});

generatorGroup.Channels.AddRange(famosFile.Fields[0].GetChannels().Take(4));

// add other elements to top level (no group)
famosFile.Texts.Add(new FamosFileText("Random list of texts.", new List<string>() { "Text 1.", "Text 2?", "Text 3!" }));
famosFile.Channels.Add(famosFile.Fields[0].GetChannels().Last());

// Save file.
famosFile.Save("sample.dat", FileMode.Create, writer => 
{
    // write your data here: famosFile.WriteSingle(writer, component, data);
});
```