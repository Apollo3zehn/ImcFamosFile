using ImcFamosFile;

namespace FamosFileSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // prepare group
            var group = new FamosFileGroup() // remove index requirement
            {
                Name = "My first group.",
                Comment = "My group comment.",
            };

            /* single values */
            group.SingleValues.Add(new FamosFileSingleValue() // allow passing a byte array as value
            {
                 Name = "My first single value.",
                 Comment = "Single value comment.",
                 DataType = FamosFileDataType.Int16,
                 Unit = "My first unit.",
                 Value = 1234,
                 Time = 111110111,
            });

            /* texts */
            group.Texts.Add(new FamosFileText() // v1 vs. v2
            {
                Name = "Name",
                Comment = "Comment",
                Text = "Text"
            });

            // prepare famos file and add group to it
            var famosFile = new FamosFile()
            {
                DataOrigin = FamosFileDataOrigin.Calculated,
                Comment = "My first comment.",
            };

            famosFile.Groups.Add(group);

            famosFile.Save("TestData.dat");
        }
    }
}
