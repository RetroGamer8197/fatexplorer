using System.IO;

namespace fatimageexplorer
{
    public class BiosParameterBlock
    {
        public readonly string OEM_Identifier = "";
        public readonly ushort BytesPerSector;
        public readonly byte SectorsPerCluster;
        public readonly ushort ReservedSectorsCount;
        public readonly byte FAT_Count;
        public readonly ushort RootDirectoryEntries;
        public readonly ushort SectorsInLogicalVolume;
        public readonly byte MediaDescriptorType;
        public readonly ushort short_NumberOfSectorsPerFAT;
        public readonly ushort SectorsPerTrack;
        public readonly ushort HeadCount;
        public readonly uint HiddenSectorCount;
        public readonly uint long_NumberOfSectorsPerFAT;
        public readonly FAT_Type_enum FAT_Type;
        
        public readonly EBPB_struct EBPB;
        public struct EBPB_struct
        {
            public byte DriveNumber;
            public byte NT_Flags;
            public byte Signature;
            public uint VolumeID;
            public string VolumeLabel;
            public string SystemIdentifierString;
        }
        public enum FAT_Type_enum
        {
            FAT12, FAT16, FAT32
        }

        public BiosParameterBlock(ref BinaryReader binaryReader)
        {
            OEM_Identifier                  = ImageFileAccessTools.ReadString(8, ref binaryReader);
            BytesPerSector                  = binaryReader.ReadUInt16();
            SectorsPerCluster               = binaryReader.ReadByte();
            ReservedSectorsCount            = binaryReader.ReadUInt16();
            FAT_Count                       = binaryReader.ReadByte();
            RootDirectoryEntries            = binaryReader.ReadUInt16();
            SectorsInLogicalVolume          = binaryReader.ReadUInt16();
            MediaDescriptorType             = binaryReader.ReadByte();
            short_NumberOfSectorsPerFAT     = binaryReader.ReadUInt16();
            SectorsPerTrack                 = binaryReader.ReadUInt16();
            HeadCount                       = binaryReader.ReadUInt16();
            HiddenSectorCount               = binaryReader.ReadUInt32();
            long_NumberOfSectorsPerFAT      = binaryReader.ReadUInt32();

            if (long_NumberOfSectorsPerFAT / SectorsPerCluster >= 65525)
            {
                //FAT 32
                FAT_Type = FAT_Type_enum.FAT32;
            } else
            {
                if (SectorsInLogicalVolume / SectorsPerCluster < 4085)
                {
                    FAT_Type = FAT_Type_enum.FAT12;
                } 
                else
                {
                    FAT_Type = FAT_Type_enum.FAT16;
                }

                EBPB = new()
                {
                    DriveNumber             = binaryReader.ReadByte(),
                    NT_Flags                = binaryReader.ReadByte(),
                    Signature               = binaryReader.ReadByte(),
                    VolumeID                = binaryReader.ReadUInt32(),
                    VolumeLabel             = ImageFileAccessTools.ReadString(11, ref binaryReader),
                    SystemIdentifierString  = ImageFileAccessTools.ReadString(8, ref binaryReader),
                };
            }
        }
    }
}