using System;
using System.IO;

namespace fatimageexplorer
{
    public class DirectoryEntry
    {
        public uint startCluster;
        public string FileName;
        public string FileExtension;
        public byte Attributes_byte; public Attributes_struct Attributes;
        public byte NT_Reserved;
        public byte CreationTime_HundredthSecond;
        public ushort CreationTime_HMS;
        public ushort CreationDate;
        public ushort LastAccessedDate;
        public ushort HighFirstClusterNumber;
        public ushort ModificationTime;
        public ushort ModificationDate;
        public ushort LowFirstClusterNumber;
        public uint FileSizeB;

        public readonly int index;

        public DirectoryEntry(ref BinaryReader binaryReader, int _index)
        {
            index = _index;

            FileName    = ImageFileAccessTools.ReadString(8, ref binaryReader);
            FileExtension = ImageFileAccessTools.ReadString(3, ref binaryReader);
            Attributes_byte = binaryReader.ReadByte();
            NT_Reserved = binaryReader.ReadByte();
            CreationTime_HundredthSecond = binaryReader.ReadByte();
            CreationTime_HMS = binaryReader.ReadUInt16();
            CreationDate = binaryReader.ReadUInt16();
            LastAccessedDate = binaryReader.ReadUInt16();
            HighFirstClusterNumber = binaryReader.ReadUInt16();
            ModificationTime = binaryReader.ReadUInt16();
            ModificationDate = binaryReader.ReadUInt16();
            LowFirstClusterNumber = binaryReader.ReadUInt16();
            FileSizeB = binaryReader.ReadUInt32();

            Attributes = ConvertToAttributes(Attributes_byte);

            startCluster = (uint)(HighFirstClusterNumber << 16) + LowFirstClusterNumber;
        }

        public DirectoryEntry()
        {
            FileName = "        ";
            FileExtension = "   ";
        }

        public static byte[] GenerateFileEntry(string _filename, string _extension, byte _attributes, byte _NT_Reserved, uint firstClusterNumber, uint _file_size_B)
        {
            byte[] fileEntry = new byte[32];

            for (int i = 0; i < 8; i++)
            {
                if (i < _filename.Length)
                {
                    fileEntry[i] = (byte)_filename[i];
                } else
                {
                    fileEntry[i] = 0x20;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (i < _extension.Length)
                {
                    fileEntry[i+8] = (byte)_extension[i];
                } else
                {
                    fileEntry[i+8] = 0x20;
                }
            }

            fileEntry[11] = _attributes;
            fileEntry[12] = _NT_Reserved;
            fileEntry[13] = (byte)(100 * DateTime.Now.Second % 2);

            ushort time = (ushort)((DateTime.Now.Second / 2) + (DateTime.Now.Minute << 5) + (DateTime.Now.Hour << 11));
            fileEntry[14] = (byte)(time % 256);
            fileEntry[15] = (byte)(time >> 8);

            ushort date;

            date = (ushort)((DateTime.Now.Day) + (DateTime.Now.Month << 5) + ((DateTime.Now.Year - 1980) << 9));
            fileEntry[16] = (byte)(date % 256);
            fileEntry[17] = (byte)(date >> 8);

            date = (ushort)((DateTime.Now.Day) + (DateTime.Now.Month << 5) + ((DateTime.Now.Year - 1980) << 9));
            fileEntry[18] = (byte)(date % 256);
            fileEntry[19] = (byte)(date >> 8);
            
            fileEntry[20] = (byte)((firstClusterNumber >> 16) % 256);
            fileEntry[21] = (byte)(firstClusterNumber >> 24);
            
            date = (ushort)((DateTime.Now.Day) + (DateTime.Now.Month << 5) + ((DateTime.Now.Year - 1980) << 9));
            fileEntry[22] = (byte)(date % 256);
            fileEntry[23] = (byte)(date >> 8);

            time = (ushort)((DateTime.Now.Second / 2) + (DateTime.Now.Minute << 5) + (DateTime.Now.Hour << 11));
            fileEntry[24] = (byte)(time % 256);
            fileEntry[25] = (byte)(time >> 8);

            fileEntry[26] = (byte)((firstClusterNumber) % 256);
            fileEntry[27] = (byte)((firstClusterNumber >> 8) % 256);

            fileEntry[28] = (byte)((_file_size_B) % 256);
            fileEntry[29] = (byte)((_file_size_B >> 8) % 256);
            fileEntry[30] = (byte)((_file_size_B >> 16) % 256);
            fileEntry[31] = (byte)(_file_size_B >> 24);

            return fileEntry;
        }

        public string GetCreationDateAndTime()
        {
            float year, month, day, hour, minute, second;

            year = (CreationDate >> 9) + 1980u;
            month = (CreationDate >> 5) & 0xF;
            day = CreationDate & 0x1F;

            hour = CreationTime_HMS >> 11;
            minute = (CreationDate >> 5) & 0x3F;
            second = float.Round(2 * (CreationDate & 0x1F) + (CreationTime_HundredthSecond / 100));

            return day + "/" + month + "/" + year + "  " + hour + ":" + minute + ":" + second;
        }

        public struct Attributes_struct
        {
            public bool READ_ONLY, HIDDEN, SYSTEM, VOLUME_ID, DIRECTORY, ARCHIVE;
        }

        private static Attributes_struct ConvertToAttributes(byte attributes_byte)
        {
            Attributes_struct attributes = new()
            {
                READ_ONLY   = Convert.ToBoolean(attributes_byte & 0x01),
                HIDDEN      = Convert.ToBoolean(attributes_byte & 0x02),
                SYSTEM      = Convert.ToBoolean(attributes_byte & 0x04),
                VOLUME_ID   = Convert.ToBoolean(attributes_byte & 0x08),
                DIRECTORY   = Convert.ToBoolean(attributes_byte & 0x10),
                ARCHIVE     = Convert.ToBoolean(attributes_byte & 0x20)
            };

            return attributes;
        }
    }

    public class LFN_Entry
    {
        public byte Index;
        public string FileName = "";
        public byte Attribute;
        public byte LongEntryType;
        public byte SFN_Checksum;
        public LFN_Entry(DirectoryEntry directoryEntry)
        {
            Index = (byte)directoryEntry.FileName[0];
            for (int i = 0; i < 3; i++)
            {
                FileName += Convert.ToChar(directoryEntry.FileName[1 + (2 * i)] + (256* directoryEntry.FileName[2 + (2 * i)]));    
            }
            FileName += Convert.ToChar(directoryEntry.FileName[7] + (256* directoryEntry.FileExtension[0]));
            FileName += Convert.ToChar(directoryEntry.FileExtension[1] + (256* directoryEntry.FileExtension[2]));
            Attribute = directoryEntry.Attributes_byte;
            LongEntryType = directoryEntry.NT_Reserved;
            SFN_Checksum = directoryEntry.CreationTime_HundredthSecond;
            FileName += Convert.ToChar(directoryEntry.CreationTime_HMS);
            FileName += Convert.ToChar(directoryEntry.CreationDate);
            FileName += Convert.ToChar(directoryEntry.LastAccessedDate);
            FileName += Convert.ToChar(directoryEntry.HighFirstClusterNumber);
            FileName += Convert.ToChar(directoryEntry.ModificationTime);
            FileName += Convert.ToChar(directoryEntry.ModificationDate);
            FileName += Convert.ToChar((ushort)(directoryEntry.FileSizeB >> 16));
            FileName += Convert.ToChar((ushort)(directoryEntry.FileSizeB % 65536));
        }
    }
}