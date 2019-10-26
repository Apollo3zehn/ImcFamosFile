# Introduction

This package allows to read and write imc FAMOS files of version 2. Simply start a new .NET Core project with the `ImcFamosFile` package installed:

```powershell
PS> dotnet new console
PS> dotnet add package ImcFamosFile -v 1.0.0-preview.1.final
```

Then, open the file:

```cs
var famosFile = FamosFile.Open("<path to file>");
```

The returning type exposes the following [properties](xref:ImcFamosFile.FamosFileHeader):

- `Channels[]` - A [channel](xref:ImcFamosFile.FamosFileChannel) is a named [component](xref:ImcFamosFile.FamosFileComponent). A component can be associated to more than one channel, i.e. there may be one channel per bit in a two byte (16-bit) component. Or you can use two channels for a single component but assign them to different groups.

- `CustomKeys[]` - Can be used to store additional [custom data](xref:ImcFamosFile.FamosFileCustomKey) in the file.

- `OriginInfo` - Contains information about the data [origin](xref:ImcFamosFile.FamosFileOriginInfo).

- `Fields[]` - A [field](xref:ImcFamosFile.FamosFileField) is a collection of components, i.e. one component represents the `y-axis` data and a second component contains the associated `x-axis` (e.g. time data). The exact field layout depends on the [field type](xref:ImcFamosFile.FamosFileFieldType).

- `Groups[]` - [Groups](xref:ImcFamosFile.FamosFileGroup) are used to combine related channels, single values and texts.

- `LanguageInfo` - The language info is important to correctly interpret special characters like `°C`. The FAMOS file format v2 does not support UTF-8 coding but instead requires you to specifiy a code page. The default code page is the commonly used [Windows-1252](https://en.wikipedia.org/wiki/Windows-1252).

- `RawBlocks` - A [raw block](xref:ImcFamosFile.FamosFileRawBlock) contains the raw data. A file can contain more than on raw block, but typically a single one is sufficient.

- `SingleValues` - A [single value](xref:ImcFamosFile.FamosFileSingleValue) is - as the name suggests - a named single value like the standard deviation of a channel.

- `Texts` - A [text](xref:ImcFamosFile.FamosFileText) can contain a single named text or a list of multiple texts

There are many more types and properties (most of them belong to the components), which are set to meaningful default values. You can manually edit all properties when creating FAMOS files but their meaning should be understood first. A format description is provided by imc itself in their document [Manual for imc Shared Components](https://www.imc-tm.com/download-center/product-downloads/imc-shared-components/). Unfortunately, the format description is incomplete.

In general, a FAMOS file contains a list of fields, which combines one or more components. When the [field type](xref:ImcFamosFile.FamosFileFieldType) is `FamosFileFieldType.MultipleYToSingleEquidistantTime`, a field may consist of multiple `y-axis` datasets without any specific `x-axis` data. This is the standard for equidistant time data, where it is sufficient to document the start time and sample rate. In this case, the field contains multiple components of type `FamosFileComponentType.Primary`.

When the time data are not equidistant (i.e. `FamosFileFieldType.MultipleYToSingleMonotonousTime`), the field contains multiple `y-axis` datasets (`FamosFileComponentType.Primary`) and a single `x-axis` dataset (`FamosFileComponentType.Secondary`).

There are more field types to represent other types of data like characteristic curves and complex data as described [here](xref:ImcFamosFile.FamosFileFieldType).

So, on top level, there are field which contains components. These components in turn may be associated to one or more channels. Only channels are display in FAMOS.

When you have a field to represent two `y-axis` datasets and a single time axis `x-axis`, the field consists of three components. Typically you would like to display the two `y-axis` components in FAMOS. In that case, create two channels and assign them to the `y-axis` components. Famos will automatically detect that the `x-axis` belongs to the channels.

You can use [groups](xref:ImcFamosFile.FamosFileGroup) to group related channels together. When a channel is not part to any group, it is display on the top level within FAMOS.

If you assign two channels to a single component, it will be displayed twice in FAMOS, which makes no sense in the first place. But if you assign both channels to different groups, you are able to link the data more than once to structure your data.

There are many more types to describe the components in detail and type to describe the whole dataset.

Two of them are [texts](xref:ImcFamosFile.FamosFileText) and [single values](xref:ImcFamosFile.FamosFileSingleValue). Texts are helpful to add a short description to a group or on top level. Single values typically contain statistical data like mean and standard deviation but may also contain simple parameters.

The following pages describe how to interact with ImcFamosFile.