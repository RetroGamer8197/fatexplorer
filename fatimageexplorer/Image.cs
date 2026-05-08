using System.IO;
using System.Collections.Generic;
using System;
using System.Reflection.Metadata.Ecma335;

namespace fatimageexplorer
{
    public class HDD_Image : Image
    {
        private BinaryReader binaryReader;

        public HDD_Image(string filename)
        {
            binaryReader = new(File.Open(filename, FileMode.OpenOrCreate));
            imageMBR = new(ref binaryReader);
            binaryReader.Close();

            int i = 1;

            foreach (PartitionEntry entry in imageMBR.partitionEntries)
            {
                if (/*entry.status != 0x00 &&*/(entry.partitionType == 0x01 || entry.partitionType == 0x04 || entry.partitionType == 0x06) )
                {
                    partitions.Add(new Partition(filename, entry, out bool success, i));
                }
                i++;
            }
        }
    }

    public class Image
    {
        public MBR imageMBR;
        public List<Partition> partitions = [];

        public static Image OpenImage(string filename)
        {
            BinaryReader binaryReader = new(File.Open(filename, FileMode.OpenOrCreate));
            byte firstByte = binaryReader.ReadByte();
            /*binaryReader.BaseStream.Seek(3, SeekOrigin.Begin);
            BiosParameterBlock bpb_test = new(ref binaryReader);*/
            binaryReader.Close();
            if (firstByte == 0xEB)
            {
                return new Floppy_Image(filename);
            }
            if (firstByte == 0xFA)
            {
                return new HDD_Image(filename);
            }
            return null;
        }
    }

    public class Floppy_Image : Image
    {
        public Floppy_Image(string filename) {
            Stream file = File.Open(filename, FileMode.OpenOrCreate);
            int length = (int)file.Length / 512;
            file.Close();
            partitions.Add(new(filename, new PartitionEntry(0, length), out bool success, 1));
        }
    }
    
    public abstract class ImageFileAccessTools
    {
        public static string ReadString(int length, ref BinaryReader binaryReader)
        {
            string result = "";

            for (int i = 0; i < length; i++)
            {
                result += (char)binaryReader.ReadByte();
            }

            return result;
        }
    }
}