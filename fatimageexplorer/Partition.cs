using System.IO;
using System.Collections.Generic;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Formats.Asn1;

namespace fatimageexplorer
{
    public class Partition
    {
        private string filename;
        private BiosParameterBlock BPB;
        private PartitionEntry partitionEntry;
        private BinaryReader binaryReader;
        public Stack<ParentDirEntry> ParentDirs = [];
        ParentDirEntry currentDirEntry;

        public List<DirectoryEntry> rootDirectory;
        public List<DirectoryEntry> rootDirectorySubdirectories;

        public List<DirectoryEntry> currentDirectory;
        public List<DirectoryEntry> currentDirectorySubdirectories;

        private uint dataStart;
        private uint FAT_start;
        private uint PartitionStart;
        private uint rootDirectorySectors;
        private uint SectorsPerFAT;
        public readonly int index;

        public Partition(string _filename, PartitionEntry _partitionEntry, out bool success, int _index) 
        {
            success = true;
            index = _index;

            filename = _filename;
            binaryReader = new(File.Open(filename, FileMode.Open));
            binaryReader.BaseStream.Seek(_partitionEntry.LBA_first_sector * 512, SeekOrigin.Begin);
            PartitionStart = _partitionEntry.LBA_first_sector * 512u;
            partitionEntry = _partitionEntry;
            byte[] jumpFlags = [binaryReader.ReadByte(), binaryReader.ReadByte(), binaryReader.ReadByte()];

            if (jumpFlags[0] == 0xEB && jumpFlags[2] == 0x90)
            {
                BPB = new(ref binaryReader);
            } else
            {
                success = false;
                return;
            }
            
            rootDirectory = [];
            rootDirectorySubdirectories = [];

            if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12 || BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT16)
            {
                if (BPB.short_NumberOfSectorsPerFAT == 0)
                {
                    SectorsPerFAT = BPB.long_NumberOfSectorsPerFAT;
                } else
                {
                    SectorsPerFAT = BPB.short_NumberOfSectorsPerFAT;
                }
                rootDirectorySectors = ((BPB.RootDirectoryEntries * 32u) + (BPB.BytesPerSector - 1u)) / BPB.BytesPerSector;
                dataStart = PartitionStart + BPB.BytesPerSector * (BPB.ReservedSectorsCount + (BPB.FAT_Count * SectorsPerFAT) + rootDirectorySectors);
                FAT_start = PartitionStart + (uint)(BPB.BytesPerSector * BPB.ReservedSectorsCount);
                
                binaryReader.BaseStream.Seek(dataStart - (BPB.BytesPerSector * rootDirectorySectors), SeekOrigin.Begin);

                DirectoryEntry tempEntry;
                for (int i = 0; i < BPB.RootDirectoryEntries; i++)
                {
                    tempEntry = new(ref binaryReader, i);
                    if (tempEntry.FileSizeB != 0)
                    {
                        rootDirectory.Add(tempEntry);
                    }
                    if (tempEntry.Attributes.DIRECTORY == true)
                    {
                        rootDirectorySubdirectories.Add(tempEntry);
                    }
                }
            }

            CopyRootEntriesToCurrentDir();

            currentDirEntry = new() {startCluster = -1, directoryEntries = BPB.RootDirectoryEntries};

            binaryReader.Close();
        }

        public string GetName()
        {
            if (BPB == null )
            {
                return "";
            } else
            {
                return BPB.EBPB.VolumeLabel;
            }
        }

        private void CopyRootEntriesToCurrentDir()
        {
            currentDirectory = [];
            foreach (DirectoryEntry entry in rootDirectory)
            {
                currentDirectory.Add(entry);
            }
            currentDirectorySubdirectories = [];
            foreach (DirectoryEntry entry in rootDirectorySubdirectories)
            {
                currentDirectorySubdirectories.Add(entry);
            }
        }

        public DirectoryEntry GetDirectoryEntryByName(string filename)
        {
            foreach (DirectoryEntry entry in currentDirectory)
            {
                if (entry.FileName + "." + entry.FileExtension == filename)
                {
                    return entry;
                }
            }

            foreach (DirectoryEntry entry in currentDirectorySubdirectories)
            {
                if (entry.FileName + entry.FileExtension == filename)
                {
                    return entry;
                }
            }
            return null;
        }

        public void DisplayBPB_Properties()
        {
            Console.WriteLine("OEM Identifier:\t\t" + BPB.OEM_Identifier);
            Console.WriteLine("Bytes per sector:\t" + BPB.BytesPerSector);
            Console.WriteLine("Sectors per cluster:\t" + BPB.SectorsPerCluster);
            Console.WriteLine("Reserved Sectors:\t" + BPB.ReservedSectorsCount);
            Console.WriteLine("Volume Label:\t\t" + BPB.EBPB.VolumeLabel);
        }

        public void ExtractFile(string extractedName, DirectoryEntry file, bool ignoreFileSize)
        {
            if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12 || BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT16)
            {
                FAT12_16_ExtractFile(extractedName, file, ignoreFileSize);
            } else
            {
                
            }
             
        }

        public void PopulateSubdir(ParentDirEntry _currentDirEntry)
        {
            if (_currentDirEntry.startCluster == -1)
            {
                CopyRootEntriesToCurrentDir();
                currentDirEntry = _currentDirEntry;
            } else
            {
                currentDirectory = [];
                currentDirectorySubdirectories = [];
                binaryReader = new(File.Open(filename, FileMode.OpenOrCreate));
                binaryReader.BaseStream.Seek(ClusterNumberToByte((uint)_currentDirEntry.startCluster), SeekOrigin.Begin);

                DirectoryEntry tempEntry;
                for (int i = 0; i < _currentDirEntry.directoryEntries; i++)
                {
                    tempEntry = new(ref binaryReader, i);
                    if (!(tempEntry.FileName == ".       " || tempEntry.FileName == "..      "))
                    {
                        if (tempEntry.FileSizeB != 0)
                        {
                            currentDirectory.Add(tempEntry);
                        }
                        if (tempEntry.Attributes.DIRECTORY == true)
                        {
                            currentDirectorySubdirectories.Add(tempEntry);
                        }
                    }
                }

                binaryReader.Close();
            }
        }

        public void PopulateSubdir(ParentDirEntry _currentDirEntry, string directoryListing)
        {
            if (_currentDirEntry.startCluster == -1)
            {
                CopyRootEntriesToCurrentDir();
                currentDirEntry = _currentDirEntry;
            } else
            {
                currentDirectory = [];
                currentDirectorySubdirectories = [];
                binaryReader = new(File.Open(directoryListing, FileMode.OpenOrCreate));

                DirectoryEntry tempEntry;
                int i = 0;
                while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                {
                    tempEntry = new(ref binaryReader, i);
                    if (!(tempEntry.FileName == ".       " || tempEntry.FileName == "..      "))
                    {
                        if (tempEntry.Attributes.HIDDEN || tempEntry.FileExtension[2] == 0x0F)
                        {
                            
                        }
                        else if (tempEntry.FileSizeB != 0)
                        {
                            currentDirectory.Add(tempEntry);
                        }
                        else if (tempEntry.Attributes.DIRECTORY == true)
                        {
                            currentDirectorySubdirectories.Add(tempEntry);
                        }
                    }
                }

                binaryReader.Close();
            }
        }

        public void OpenSubdir(DirectoryEntry entry)
        {
            ParentDirs.Push(currentDirEntry);
            ExtractFile("currentdir", entry, true);
            currentDirEntry = new(){startCluster = (int)entry.startCluster, directoryEntries = 256};
            PopulateSubdir(currentDirEntry, "currentdir");
        }

        private void FAT12_16_ExtractFile(string extractedName, DirectoryEntry file, bool ignoreFileSize)
        {
            bool finishedRead = false;
            uint bytesRead = 0;
            uint currentClusterNumber = file.startCluster;
            int sectorsFromThisCluster;
            BinaryWriter binaryWriter = new(File.Open(extractedName, FileMode.Create));
            binaryReader = new(File.Open(filename, FileMode.OpenOrCreate));
            uint fileEndValue = 0;

            if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12)
            {
                fileEndValue = 0xFF8;
            } else if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT16)
            {
                fileEndValue = 0xFFF8;
            }

            uint maxFileSize;

            if (ignoreFileSize)
            {
                maxFileSize = uint.MaxValue;
            } else
            {
                maxFileSize = file.FileSizeB;
            }

            while (bytesRead < maxFileSize && !finishedRead)
            {
                sectorsFromThisCluster = 0;
                if (ignoreFileSize)
                {
                    if (currentClusterNumber > fileEndValue)
                    {
                        binaryWriter.Close();
                        binaryReader.Close();
                        return;
                    }
                }
                binaryReader.BaseStream.Seek((long)ClusterNumberToByte(currentClusterNumber), SeekOrigin.Begin);
                while (sectorsFromThisCluster < BPB.SectorsPerCluster && !finishedRead)
                {
                    if (maxFileSize - bytesRead > BPB.BytesPerSector)
                    {
                        bytesRead += BPB.BytesPerSector;
                        binaryWriter.Write(binaryReader.ReadBytes(BPB.BytesPerSector));
                    } else
                    {
                        binaryWriter.Write(binaryReader.ReadBytes((int)((maxFileSize - bytesRead) % int.MaxValue)));
                        bytesRead += file.FileSizeB - bytesRead;
                        finishedRead = true;
                    }
                    sectorsFromThisCluster++;
                }
                if (!finishedRead)
                {
                    currentClusterNumber = GetNextCluster(currentClusterNumber);
                }
            }

            binaryWriter.Close();
            binaryReader.Close();
        }

        public void InjectFile(string filePath, string newName, string newExtension)
        {
            BinaryReader newFile_reader = new BinaryReader(File.Open(filePath, FileMode.OpenOrCreate));
            List<byte> new_file = [];
            for (int i = 0; i < newFile_reader.BaseStream.Length; i++)
            {
                new_file.Add(newFile_reader.ReadByte());
            }

            newFile_reader.Close();

            /*binaryReader = new(File.Open(filename, FileMode.OpenOrCreate));
            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
            List<byte> image_File = [];
            for (long i = 0; i < binaryReader.BaseStream.Length; i++)
            {
                image_File.Add(binaryReader.ReadByte());
            }
            binaryReader.Close();*/

            File.Copy(filename, "exported.bin", true);

            binaryReader = new(File.Open("exported.bin", FileMode.OpenOrCreate));

            if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12)
            {
                UpdateClusterValue(ref binaryReader, 0, 0xFF0);
            } else if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT16)
            {
                UpdateClusterValue(ref binaryReader, 0, 0xFFF0);
            }
            

            // look for an empty entry
            uint currentSeekIndex = dataStart - BPB.BytesPerSector * rootDirectorySectors - 32;
            byte currentEntryFlag = 0xFF;
            while (currentEntryFlag != 0xE5 && currentEntryFlag != 0x00)
            {
                currentSeekIndex += 32;
                binaryReader.BaseStream.Seek(currentSeekIndex, SeekOrigin.Begin);
                currentEntryFlag = binaryReader.ReadByte();
            }

            BinaryWriter exporter = new(binaryReader.BaseStream);

            int entryStart = (int)currentSeekIndex;
            int file_fat_cluster = LookForEmptyCluster(ref binaryReader);
            byte[] file_entry = DirectoryEntry.GenerateFileEntry(newName, newExtension, 0x00, 0x00, (uint)file_fat_cluster, (uint)new_file.Count);
            exporter.Seek(entryStart, SeekOrigin.Begin);
            exporter.Write(file_entry);

            int bytesLeft = new_file.Count;

            int sectorsNeeded = (new_file.Count + 511) / 512;
            int sectorsUsed = 0;
            for (int i = 0; i < sectorsNeeded; i++)
            {
                int file_start = (int)ClusterNumberToByte((uint)file_fat_cluster);
                int index = 0;
                exporter.Seek(file_start, SeekOrigin.Begin);
                while (index < BPB.BytesPerSector * BPB.SectorsPerCluster && bytesLeft > 0)
                {
                    if (bytesLeft >= BPB.BytesPerSector)
                    {
                        exporter.Write(new_file[index..(index+BPB.BytesPerSector)].ToArray());
                        index += BPB.BytesPerSector;
                        bytesLeft -= BPB.BytesPerSector;
                    } else
                    {
                        exporter.Write(new_file[index..(index+bytesLeft)].ToArray());
                        index+=bytesLeft;
                        bytesLeft = 0;
                    }
                }
                if (sectorsNeeded - i > 1)
                {
                    if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12)
                    {
                        UpdateClusterValue(ref binaryReader, file_fat_cluster, 0xFF8);
                    } 
                    else if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT16)
                    {
                        UpdateClusterValue(ref binaryReader, file_fat_cluster, 0xFFF8);
                    }
                    ushort next_file_fat_cluster = (ushort)LookForEmptyCluster(ref binaryReader);
                    UpdateClusterValue(ref binaryReader, file_fat_cluster, next_file_fat_cluster);
                    
                    file_fat_cluster = next_file_fat_cluster;
                } else
                {
                    if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12)
                    {
                        UpdateClusterValue(ref binaryReader, file_fat_cluster, 0xFFF);
                    } else if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT16)
                    {
                        UpdateClusterValue(ref binaryReader, file_fat_cluster, 0xFFFF);
                    }
                }
                sectorsUsed++;
            }

            binaryReader.Close();
            exporter.Close();

            
        }

        public void Export(string newfilename)
        {
            BinaryWriter binaryWriter = new(File.Open(newfilename, FileMode.Create));
            binaryReader = new(File.Open(filename, FileMode.OpenOrCreate));

            binaryWriter.Write(binaryReader.ReadBytes((int)binaryReader.BaseStream.Length));

            binaryReader.Close();
            binaryWriter.Close();
        }

        public void Delete(string filename_toDelete)
        {
            bool found = false;
            DirectoryEntry entryToDelete = new();

            File.Copy(filename, "exported.bin", true);

            binaryReader = new(File.Open("exported.bin", FileMode.OpenOrCreate));
            foreach (DirectoryEntry entry in rootDirectory)
            {
                if (found)
                {
                    
                }
                else if (entry.FileName + "." + entry.FileExtension == filename_toDelete)
                {
                    found = true;
                    entryToDelete = entry;
                }
            }
            if (!found)
            {
                return;
            }
            ushort maxCluster = 0xFF0;
            if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12)
            {
                maxCluster = 0xFF0;
            } else
            {
                maxCluster = 0xFFF0;
            }
            uint currentCluster = entryToDelete.startCluster;
            uint nextCluster;

            do
            {
                nextCluster = GetNextCluster(currentCluster);
                UpdateClusterValue(ref binaryReader, (int)currentCluster, 0);
                currentCluster = nextCluster;
            } while (currentCluster < maxCluster);

            uint entryStart = (uint)(dataStart - BPB.BytesPerSector * rootDirectorySectors + (entryToDelete.index * 32));
            BinaryWriter binaryWriter = new(binaryReader.BaseStream);
            binaryWriter.Seek((int)entryStart, SeekOrigin.Begin);

            binaryWriter.Write(new byte[32]);
            binaryWriter.Close();
            binaryReader.Close();
        }

        private uint ClusterNumberToByte(uint clusterNumber)
        {
            uint byteIndex = dataStart + ((clusterNumber - 2) * BPB.BytesPerSector * BPB.SectorsPerCluster);

            return byteIndex;
        }

        private uint GetNextCluster(uint currentClusterNumber)
        {
            if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12)
            {
                ushort cluster;
                binaryReader.BaseStream.Seek(FAT_start + currentClusterNumber * 3 / 2, SeekOrigin.Begin);
                cluster = binaryReader.ReadUInt16();
                if (currentClusterNumber % 2 == 1)
                {
                    cluster >>= 4;
                } else
                {
                    cluster &= 0xFFF;
                }

                return cluster;
            } 
            else if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT16)
            {
                ushort cluster;
                binaryReader.BaseStream.Seek((long)(FAT_start + (currentClusterNumber * 2)), SeekOrigin.Begin);
                cluster = binaryReader.ReadUInt16();
                return cluster;
            }

            return 0;
        }

        private int LookForEmptyCluster(ref BinaryReader image)
        {
            if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT12)
            {
                return FAT12_LookForEmptyCluster(ref image);
            }
            if (BPB.FAT_Type == BiosParameterBlock.FAT_Type_enum.FAT16)
            {
                return FAT16_LookForEmptyCluster(ref image);
            }
            else
            {
                return -1;
            }

        }

        private int FAT12_LookForEmptyCluster(ref BinaryReader image)
        {
            int currentClusterNumber = -1;
            ushort currentClusterValue = 0xFFF;
            ushort oldClusterValue = 0;
            int imageIndex;
            while (currentClusterValue != 0x000 && currentClusterNumber < 0xFF0)
            {
                currentClusterNumber++;
                imageIndex = (int)FAT_start + (currentClusterNumber * 3 / 2);
                binaryReader.BaseStream.Seek(imageIndex, SeekOrigin.Begin);

                byte lowB = binaryReader.ReadByte(), highB = binaryReader.ReadByte();
                currentClusterValue = (ushort)(lowB + (highB << 8));
                if (currentClusterNumber % 2 == 1)
                {
                    oldClusterValue = currentClusterValue;
                    currentClusterValue >>= 4;
                } else
                {
                    oldClusterValue = currentClusterValue;
                    currentClusterValue &= 0xFFF;
                }
            }
            return currentClusterNumber;
        }

        private int FAT16_LookForEmptyCluster(ref BinaryReader image)
        {
            int currentClusterNumber = -1;
            ushort currentClusterValue = 0xFFFF;
            ushort oldClusterValue = 0;
            int imageIndex;
            while (currentClusterValue != 0x0000 && currentClusterNumber < 0xFFF0)
            {
                currentClusterNumber++;
                imageIndex = (int)FAT_start + (currentClusterNumber * 2);

                binaryReader.BaseStream.Seek(imageIndex, SeekOrigin.Begin);

                byte lowB = binaryReader.ReadByte(), highB = binaryReader.ReadByte();

                currentClusterValue = (ushort)(lowB + (highB << 8));
            }
            return currentClusterNumber;
        }

        private ushort UpdateClusterValue(ref BinaryReader binaryReader, int file_fat_cluster, ushort nextCluster)
        {
            int imageIndex = (int)FAT_start + (file_fat_cluster * 3 / 2);
            binaryReader.BaseStream.Seek(imageIndex, SeekOrigin.Begin);

            byte firstByte = binaryReader.ReadByte();
            byte secondByte = binaryReader.ReadByte();

            ushort clusterValue = (ushort)(firstByte + (secondByte << 8));

            if (file_fat_cluster % 2 == 0)
            {
                firstByte = (byte)(nextCluster & 0xFF);
                secondByte &= 0xF0;
                secondByte |= (byte)((nextCluster >> 8) & 0x00F);
            } else
            {
                firstByte &= 0x0F;
                firstByte |= (byte)((nextCluster & 0x0F) << 4);
                secondByte = (byte)((nextCluster >> 4) & 0x0FF);
            }

            BinaryWriter binaryWriter = new (binaryReader.BaseStream);

            for (int i = 0; i < BPB.FAT_Count; i++)
            {
                binaryReader.BaseStream.Seek(imageIndex + (i * BPB.short_NumberOfSectorsPerFAT * BPB.BytesPerSector), SeekOrigin.Begin);
                binaryWriter.Write(firstByte);
                binaryWriter.Write(secondByte);
            }

            return clusterValue;
        }

    }

    public class PartitionEntry
    {
        public readonly byte status;
        public readonly byte firstSector_head;
        public readonly ushort firstSector_cylinder;
        public readonly byte firstSector_sector;
        public readonly byte partitionType;
        public readonly byte lastSector_head;
        public readonly ushort lastSector_cylinder;
        public readonly byte lastSector_sector;
        public readonly uint LBA_first_sector;
        public readonly uint LBA_Length;
        public PartitionEntry(ref BinaryReader binaryReader)
        {
            status = binaryReader.ReadByte();
            firstSector_head = binaryReader.ReadByte();
            firstSector_cylinder = binaryReader.ReadByte();
            firstSector_sector = binaryReader.ReadByte();
            partitionType = binaryReader.ReadByte();
            lastSector_head = binaryReader.ReadByte();
            lastSector_cylinder = binaryReader.ReadByte();
            lastSector_sector = binaryReader.ReadByte();
            LBA_first_sector = binaryReader.ReadUInt32();
            LBA_Length = binaryReader.ReadUInt32();
        }

        public PartitionEntry(int start, int length)
        {
            LBA_first_sector = (uint)start;
            LBA_Length = (uint)length;
        }
    }

    public struct ParentDirEntry
    {
        public int startCluster;
        public int directoryEntries;
    }
}