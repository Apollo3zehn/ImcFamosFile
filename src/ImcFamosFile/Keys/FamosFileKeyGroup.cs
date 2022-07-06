using System;
using System.IO;

namespace ImcFamosFile
{
    internal class FamosFileKeyGroup : FamosFileBase
    {
        #region Constructors

        internal FamosFileKeyGroup()
        {
            //
        }

        internal FamosFileKeyGroup(BinaryReader reader) : base(reader)
        {
            DeserializeKey(FamosFileKeyType.CK, expectedKeyVersion: 1, keySize =>
            {
                var unknown = DeserializeInt32();
                var keyGroupIsClosed = DeserializeInt32() == 1;

                if (!keyGroupIsClosed)
                    throw new FormatException($"The key group is not closed. This may be a hint to an interruption that occured while writing the file content to disk.");
            });
        }

        #endregion

        #region Properties

        private protected override FamosFileKeyType KeyType => FamosFileKeyType.CK;

        #endregion

        #region Methods

        internal override void Serialize(BinaryWriter writer)
        {
            var data = new object[]
            {
                1,
                0
            };

            SerializeKey(writer, 1, data);
        }

        #endregion
    }
}
