The previous section showed how to create a component in a very short way. This is accomplished by using meaningful default values. However, the full description of a component is quite complex. It has the following properties:

- `BufferInfo` - Contains user data and a list of [buffers](../../ImcFamosFile.FamosFileBuffer.html) that describe where to find the data and how long the dataset is ([link](../../ImcFamosFile.FamosFileBufferInfo.html)).

- `Channels[]` - A list of [channels](../../ImcFamosFile.FamosFileChannel.html).

- `DisplayInfo` - Specifies how to [display](../../ImcFamosFile.FamosFileDisplayInfo.html) the data in FAMOS.

- `EventReference` - Specifies which [events](../../ImcFamosFile.FamosFileEvent.html) belong to this component ([link](../../ImcFamosFile.FamosFileEventReference.html)).

- `Name` - Returns the name of the first channel found.

- `PackInfo` - Specifies the data type and how the data are [packed](../../ImcFamosFile.FamosFilePackInfo.html).

- `TriggerTime` - A type to define when the data were created / recorded ([link](../../ImcFamosFile.FamosFileTriggerTime.html)).

- `Type` - The [component's type](../../ImcFamosFile.FamosFileComponentType.html).

- `XAXisScaling` - Scales the [x-axis](../../ImcFamosFile.FamosFileXAxisScaling.html).

- `ZAXisScaling` - Scales the [z-axis](../../ImcFamosFile.FamosFileZAxisScaling.html) and specifies if and how the axis is segmented.