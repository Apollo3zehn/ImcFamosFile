The buffers in the `BufferInfo` and the `PackInfo` properties of a component define how the data is written to and read from a file. You can modify these values by yourself, but this implementation supports two default alignment modes out of the box as described [here](../../ImcFamosFile.FamosFileAlignmentMode.html).

When you read a file using `FamosFile.Open(...)` it contains definitions for the buffers, pack infos and raw blocks so that everything is aligned properly.

When you instead create a file by yourself via `var famosFile = new FamosFileHeader();`, the instance is empty. To reduce complexity, whenever you create a component, a default buffer and pack info, and - depending on the constructor - a default channel instance is created. 

But the default values of the buffers and pack infos are not correct as they all point to the same region in the file. Thus, when you call `famosFile.Save(...)`, a data alignment is performed.

This ensures that all buffers point to unique regions in the file and - most importantly - it ensures that there is a [raw block](../../ImcFamosFile.FamosFileRawBlock.html) instance available to hold the actual data.

To disable this default mode, you need to call the save method likes this: `famosFile.Save(..., autoAlign: false)`.

In that case you are responsible to create and align the buffers correctly. The same is required when you want you data aligned in interlaced mode. In this mode, the data are written in a row-wise manner to a single large buffer which degrades performance when you want to read the data channel-wise but improves performance when you need to read the data of all channels at a specific time.

The storage procedure for interlaced data is shown here:

```cs
/* A raw block must be added manually when option 'autoAlign: false'. is set. */
famosFile.RawBlocks.Add(new FamosFileRawBlock());

/* Align buffers (lengths and offsets) to get an interlaced layout.*/
famosFile.AlignBuffers(famosFile.RawBlocks.First(), FamosFileAlignmentMode.Interlaced);

/* Save file with option 'autoAlign: false'. */
famosFile.Save("interlaced.dat", writer => Program.WriteFileContent(famosFile, writer), autoAlign: false);
```