As shown on the previous page, you can open a file like this:

```cs
var famosFile = FamosFile.Open("<path to file>");
```

The content can then be read by one of the following methods:

```cs
var allData = famosFile.ReadAll();

var group = famosFile.Groups.First();
var groupData = famosFile.Read(group.Channels);

var channel = famosFile.Channels.First();
var singleData = famosFile.ReadSingle(channel);
```

When you do not want to _read_ files but to _write_ them instead, you need to maked use of the [FamosFileHeader](../../ImcFamosFile.FamosFileHeader.html) class:

```cs
var famosFile = new FamosFileHeader();
```
There is another difference between reading and writing:

When _reading_ data, you pass one or more `channels` to the `read` methods. The returned data structure not only contains the channel's component data (`y-axis`) but also the related `x-axis` data, which is another component _without_ a channel, as described on the previous page. So the related components are determined _automagically_.

When _writing_ data, you now pass a `component` to the `write` method. This is required because not every component has a channel assigned and thus all the component' datasets can be written individually. If the API were designed to accept `channels` instead, the user would be required to provide data for both axes, `x-axis` and `y-axis`, which would be fine. But what if you have no `x-axis`? And what if you have more than on `y-axis`? In that case, the `x-axis` data would be written multiple times, which is not desirable.

In conclusion, to write data, you need to pass the components and their related data individually, which is shown in the example below.

With the freshly created `famosFile`, it is time to add your fields and components and group your channels, single values and texts and store everything to disk.

```cs
/* data field with monotonous increasing time */
famosFile.Fields.Add(new FamosFileField(FamosFileFieldType.MultipleYToSingleMonotonousTime, new List<FamosFileComponent>()
{
    /* hydraulic data */
    new FamosFileAnalogComponent("HYD_TEMP_1", FamosFileDataType.Float32, length: 10),
    new FamosFileAnalogComponent("HYD_TEMP_2", FamosFileDataType.Float32, length: 10),

    /* time-axis */
    new FamosFileAnalogComponent(FamosFileDataType.UInt32, length, FamosFileComponentType.Secondary),
});

famosFile.Save(<path to file>, writer =>
{
    var field = famosFile.Fields[0];

    famosFile.WriteSingle(writer, field.Component[0], <your y-axis 1 data>);
    famosFile.WriteSingle(writer, field.Component[1], <your y-axis 2 data>);
    famosFile.WriteSingle(writer, field.Component[2], <your x-axis data>);
});
```

The `save` method accepts an anonymous lambda method because it first writes the header data and when this is done is requests the user to provide the actual data which must match the length you specified during the component defition (`length: 10`).

See the following pages on how to create a more complex file.