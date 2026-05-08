using System.IO;

namespace fatimageexplorer
{
    public class MBR
    {
        byte[] sector = new byte[512];
        public PartitionEntry[] partitionEntries = new PartitionEntry[4];
        public MBR(ref BinaryReader binaryReader)
        {
            for (int i = 0; i < 512; i++)
            {
                sector[i] = binaryReader.ReadByte();
            }

            binaryReader.BaseStream.Seek(446, SeekOrigin.Begin);

            for (int i = 0; i < 4; i++)
            {
                partitionEntries[i] = new(ref binaryReader);
            }

            binaryReader.Close();
        }
    }
}