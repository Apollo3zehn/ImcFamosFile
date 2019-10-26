The previous section showed how to create a component in a very short way. This is accomplished by using meaningful default values. However, the full description of a component is quite complex. It has the following properties:

- `BufferInfo` - Contains user data and a list of [buffers](xref:ImcFamosFile.FamosFileBuffer) that describe where to find the data and how long the dataset is ([link](xref:ImcFamosFile.FamosFileBufferInfo)).

- `Channels[]` - A list of [channels](xref:ImcFamosFile.FamosFileChannel).

- `DisplayInfo` - Specifies how to [display](xref:ImcFamosFile.FamosFileDisplayInfo) the data in FAMOS.

- `EventReference` - Specifies which [events](xref:ImcFamosFile.FamosFileEvent) belong to this component ([link](xref:ImcFamosFile.FamosFileEventReference)).

- `Name` - Returns the name of the first channel found.

- `PackInfo` - Specifies the data type and how the data are [packed](xref:ImcFamosFile.FamosFilePackInfo).

- `TriggerTime` - A type to define when the data were created / recorded ([link](xref:ImcFamosFile.FamosFileTriggerTime)).

- `Type` - The [component's type](xref:ImcFamosFile.FamosFileComponentType).

- `XAXisScaling` - Scales the [x-axis](xref:ImcFamosFile.FamosFileXAxisScaling).

- `ZAXisScaling` - Scales the [z-axis](xref:ImcFamosFile.FamosFileZAxisScaling) and specifies if and how the axis is segmented.