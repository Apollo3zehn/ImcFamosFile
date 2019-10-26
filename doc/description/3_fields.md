As described previously, a field is a collection of one or more components:

```cs
famosFile.Fields.Add(new FamosFileField(FamosFileFieldType.MultipleYToSingleEquidistantTime, new List<FamosFileComponent>()
{
    /* generator data */
    new FamosFileAnalogComponent("GEN_TEMP_1", FamosFileDataType.Float32, length),
    new FamosFileAnalogComponent("GEN_TEMP_2", FamosFileDataType.Float32, length),
    new FamosFileAnalogComponent("GEN_TEMP_3", FamosFileDataType.Int32, length),
    new FamosFileAnalogComponent("GEN_TEMP_4", FamosFileDataType.Float32, length),

    /* environmental data */
    new FamosFileAnalogComponent("ENV_TEMP_1", FamosFileDataType.Int16, length)
})
{
    TriggerTime = new FamosFileTriggerTime(DateTime.Now, FamosFileTimeMode.Normal),
    XAxisScaling = new FamosFileXAxisScaling(deltaX: 0.01M) { X0 = 985.0M, Unit = "Seconds" }
});
```

This code creates a field with five `y-axis` components with equidistant time. When you pass a name to a component, a channel is automatically created for you. You are free to drop the name and create a channel manually, assign it to the component and to a group (e.g. `famosFile.Groups[0].Channels`) or directly to the `famosFile.Channels` property.

The `XAxisScaling` property specifies a sample rate of `100 Hz` because `deltaX` is set to `0.01` seconds. The `M` literal after a number is required because this library works with the [decimal](https://docs.microsoft.com/en-us/dotnet/api/system.decimal?view=netframework-4.8) type instead of [double](https://docs.microsoft.com/en-us/dotnet/api/system.double?view=netframework-4.8) for parameters to meet the precision requirements.

Additionally, the code assigns a `TriggerTime` to the field. This is helpful to set the global time, when the data of that field were recorded or created, respectively.

Both, the `XAxisScaling` and the `TriggerTime` (and the `ZAxisScaling` which is not shown here), apply to all components that belong to this field. Only when a component redefines one of these properties, the subsequent components will inherit the redefined properties.

Up to now, only the `x-axis` is scaled with a unit. However, it is also possible to create an analog component with calibration data:

```cs
var calibrationInfo = new FamosFileCalibration()
{
    ApplyTransformation = true,
    Factor = 10,
    Offset = 7,
    Unit = "°C"
};

var component = new FamosFileAnalogComponent(..., calibrationInfo),
```

When `ApplyTransformation` is set to `true`, the specified calibration factor and offset and applied in FAMOS. But this is only allowed for integer data!

Besides this, you can always specify the `Unit` which is display in FAMOS when plotting the data and in the channel's properties.

In general, it is important to understand that the imc FAMOS file format specification allows the storage of very simple datasets without any additional data, i.e. many properties are optional. You can easily find out which one are optional by looking at the type. This implementation uses the new `C# 8.0` feature [Nullable Reference Types](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/nullable-reference-types-specification), which prohibits to assign `null` values to normal reference types. So, by default, every field and property must be assigned. Only when you encounter a field like 

```cs
FamosFileOriginInfo? FamosFileHeader.OriginInfo { get; set;}
```

you are free to not assign any value to it.