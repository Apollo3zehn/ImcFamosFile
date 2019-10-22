using ImcFamosFile;
using System;

namespace FamosFileSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // prepare group
            var group = new FamosFileGroup("My first group.") { Comment = "My group comment." };

            /* single values */
            group.SingleValues.Add(new FamosFileSingleValue<double>("My first single value.", 1234)
            {
                 Comment = "Single value comment.",
                 Unit = "My first unit.",
                 Time = DateTime.Now,
            });

            /* texts */
            group.Texts.Add(new FamosFileText("Text") // v1 vs. v2
            {
                Name = "Name",
                Comment = "Comment"
            });

            // prepare famos file and add group to it
            var famosFile = new FamosFileHeader();

            famosFile.Groups.Add(group);

            famosFile.Save("TestData.dat", write =>
            {
                //
            });
        }
    }
}
