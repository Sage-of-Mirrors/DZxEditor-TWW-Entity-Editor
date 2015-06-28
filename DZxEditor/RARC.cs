using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GameFormatReader.Common;

namespace DZxEditor
{
    class ArcHeader
    {
        public string Magic;
        public int FileSize;
        public int Unknown1;
        public int DataOffset;

        public int Unknown2;
        public int Unknown3;
        public int Unknown4;
        public int Unknown5;

        public int NodeCount;

        public int Unknown6;
        public int Unknown7;

        public int FileEntriesOffset;

        public int Unknown8;

        public int StringTableOffset;

        public short FileEntryCount;
        public byte UnknownBool1;
        public byte Padding;
        public int Unknown10;

        public void Write(EndianBinaryWriter writer)
        {
            writer.WriteFixedString(Magic, 4);
            writer.Write(FileSize);
            writer.Write(Unknown1);
            writer.Write(DataOffset);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
            writer.Write(Unknown5);
            writer.Write(NodeCount);
            writer.Write(Unknown6);
            writer.Write(Unknown7);
            writer.Write(FileEntriesOffset);
            writer.Write(Unknown8);
            writer.Write(StringTableOffset);
            writer.Write(FileEntryCount);
            writer.Write(UnknownBool1);
            writer.Write(Padding);
            writer.Write(Unknown10);
        }
    }

    class Node
    {
        public string Type;
        public int NameOffset;

        public short NameHash;

        public short FileEntryCount;
        public int FirstFileEntryIndex;

        public void Write(EndianBinaryWriter writer)
        {
            writer.WriteFixedString(Type, 4);
            writer.Write(NameOffset);
            writer.Write(NameHash);
            writer.Write(FileEntryCount);
            writer.Write(FirstFileEntryIndex);
        }
    }

    class FileEntry
    {
        public short FileId;
        public short NameHash;
        public byte Type;
        public byte Padding;
        public short NameOffset;
        public int DataOffset;
        public int DataSize;
        public int Zero;

        public void Write(EndianBinaryWriter writer)
        {
            writer.Write(FileId);
            writer.Write(NameHash);
            writer.Write(Type);
            writer.Write(Padding);
            writer.Write(NameOffset);
            writer.Write(DataOffset);
            writer.Write(DataSize);
            writer.Write(Zero);
        }
    }

    class VirtualFolder
    {
        public string Name;

        public string NodeName;

        public List<VirtualFolder> Subdirs = new List<VirtualFolder>();

        public List<FileData> Files = new List<FileData>();
    }

    class FileData
    {
        public string Name;

        public byte[] Data;
    }

    class RARCPacker
    {
        ArcHeader Header;

        List<Node> Nodes;

        List<FileEntry> Entries;

        List<Char> StringTable;

        List<byte> Data;

        int EntryCount;

        public void Pack(VirtualFolder root, EndianBinaryWriter writer)
        {
            Header = new ArcHeader();

            Header.Magic = "RARC";

            Header.Unknown1 = 0x20;

            Header.Unknown6 = 0x20;

            Nodes = new List<Node>();

            Entries = new List<FileEntry>();

            StringTable = new List<char>();

            Data = new List<byte>();

            StringTable.Add('.');

            StringTable.Add('\0');

            StringTable.Add('.');

            StringTable.Add('.');

            StringTable.Add('\0');

            Node rootNode = new Node();

            rootNode.Type = "ROOT";

            rootNode.NameOffset = 5;

            rootNode.NameHash = HashName(root.Name);

            foreach (char c in root.Name)
            {
                StringTable.Add(c);
            }

            StringTable.Add('\0');

            rootNode.FileEntryCount = (short)(root.Subdirs.Count + root.Files.Count + 2);

            rootNode.FirstFileEntryIndex = 0;

            Nodes.Add(rootNode);

            EntryCount = rootNode.FileEntryCount;

            string lastFolderNodeName = "";

            foreach (VirtualFolder folder in root.Subdirs)
            {
                //Node subdirNode = new Node();

                //subdirNode.Type = folder.NodeName;

                //subdirNode.NameOffset = StringTable.Count;

                //foreach (char c in folder.Name)
                //{
                //    StringTable.Add(c);
                //}

                //StringTable.Add('\0');

                //rootNode.FileEntryCount = (short)(folder.Files.Count + 2);

                //Nodes.Add(subdirNode);

                //FileEntry subdirEntry = new FileEntry();

                //subdirEntry.FileId = -1;

                //subdirEntry.NameHash = HashName(folder.Name);

                //subdirEntry.Type = 2;

                //subdirEntry.Padding = 0;

                //subdirEntry.NameOffset = (short)subdirNode.NameOffset;

                //subdirEntry.DataOffset = Nodes.IndexOf(subdirNode);

                //subdirEntry.DataSize = 0x10;

                //subdirEntry.Zero = 0;

                //Entries.Add(subdirEntry);

                if (folder.NodeName == lastFolderNodeName)
                {

                }

                RecursiveDir(folder, rootNode);
            }

            foreach (FileData file in root.Files)
            {
                Entries.Add(AddFileFileEntry(file));
            }

            #region Add first two period entries. Not using the method I created because of course these two have a slightly different format....

            FileEntry singlePeriod = new FileEntry();

            singlePeriod.FileId = -1;

            singlePeriod.NameHash = 0x2E;

            singlePeriod.Type = 2;

            singlePeriod.Padding = 0;

            singlePeriod.NameOffset = 0;

            singlePeriod.DataOffset = 0;

            singlePeriod.DataSize = 0x10;

            singlePeriod.Zero = 0;

            Entries.Add(singlePeriod);

            FileEntry doublePeriod = new FileEntry();

            doublePeriod.FileId = -1;

            doublePeriod.NameHash = 0xB8;

            doublePeriod.Type = 2;

            doublePeriod.Padding = 0;

            doublePeriod.NameOffset = 2;

            doublePeriod.DataOffset = -1;

            doublePeriod.DataSize = 0x10;

            doublePeriod.Zero = 0;

            Entries.Add(doublePeriod);

            #endregion

            foreach (VirtualFolder folder in root.Subdirs)
            {
                RecursiveFile(folder);
            }

            Header.NodeCount = Nodes.Count;

            Header.Unknown2 = Data.Count;

            Header.Unknown3 = Data.Count;

            Header.Unknown7 = Entries.Count;

            Header.FileEntryCount = (short)Entries.Count;

            Header.UnknownBool1 = 1;

            int headerLength = 64;

            int unalignedData = (headerLength + (Nodes.Count * 16) + (Entries.Count * 20) + StringTable.Count);

            int alignedEntries = ((Entries.Count * 0x14) + 0x1F) & ~0x1F;

            int alignedNodes = ((Nodes.Count * 0x10) + 0x1F) & ~0x1F;

            int alignedTable = alignedNodes + alignedEntries + headerLength;

            int alignedStringTableSize = (StringTable.Count + 0x1F) & ~0x1F;

            int alignedData = (alignedTable + alignedStringTableSize + 0x1F) & ~0x1F;

            Header.Unknown8 = alignedStringTableSize;

            Header.FileEntriesOffset = alignedNodes + 64 - 0x20;

            Header.StringTableOffset = alignedTable - 0x20;

            Header.DataOffset = alignedData - 0x20;

            Header.FileSize = (alignedData + Data.Count + 0x1F) & ~0x1F;

            Write(writer);
        }

        short HashName(string name)
        {
            short hash = 0;

            short multiplier = 1;

            if (name.Length == 2)
            {
                multiplier = 2;
            }

            if (name.Length >= 3)
            {
                multiplier = 3;
            }

            foreach (char c in name)
            {
                hash = (short)(hash * multiplier);
                hash += (short)c;
            }

            return hash;
        }

        void AddPeriodEntries(int nodeIndex)
        {
            FileEntry singlePeriod = new FileEntry();

            singlePeriod.FileId = -1;

            singlePeriod.NameHash = 0x2E;

            singlePeriod.Type = 2;

            singlePeriod.Padding = 0;

            singlePeriod.NameOffset = 0;

            singlePeriod.DataOffset = nodeIndex;

            singlePeriod.DataSize = 0x10;

            singlePeriod.Zero = 0;

            Entries.Add(singlePeriod);

            FileEntry doublePeriod = new FileEntry();

            doublePeriod.FileId = -1;

            doublePeriod.NameHash = 0xB8;

            doublePeriod.Type = 2;

            doublePeriod.Padding = 0;

            doublePeriod.NameOffset = 2;

            doublePeriod.DataOffset = 0;

            doublePeriod.DataSize = 0x10;

            doublePeriod.Zero = 0;

            Entries.Add(doublePeriod);
        }

        FileEntry AddFileFileEntry(FileData file)
        {
            FileEntry entry = new FileEntry();

            entry.FileId = (short)Entries.Count;

            entry.NameHash = HashName(file.Name);

            entry.Type = 0x11;

            entry.Padding = 0;

            entry.NameOffset = (short)StringTable.Count;

            foreach (char c in file.Name)
            {
                StringTable.Add(c);
            }

            StringTable.Add('\0');

            entry.DataOffset = Data.Count;

            Data.AddRange(file.Data);

            entry.DataSize = file.Data.Length;

            entry.Zero = 0;

            return entry;
        }

        void RecursiveDir(VirtualFolder folder, Node rootNode)
        {
            Node subdirNode = new Node();

            subdirNode.Type = folder.NodeName;

            subdirNode.NameOffset = StringTable.Count;

            foreach (char c in folder.Name)
            {
                StringTable.Add(c);
            }

            subdirNode.NameHash = HashName(folder.Name);

            subdirNode.FirstFileEntryIndex = EntryCount;

            subdirNode.FileEntryCount = (short)(folder.Subdirs.Count + folder.Files.Count + 2);

            EntryCount += (folder.Subdirs.Count + folder.Files.Count + 2);

            StringTable.Add('\0');

            Nodes.Add(subdirNode);

            FileEntry subdirEntry = new FileEntry();

            subdirEntry.FileId = -1;

            subdirEntry.NameHash = HashName(folder.Name);

            subdirEntry.Type = 2;

            subdirEntry.Padding = 0;

            subdirEntry.NameOffset = (short)subdirNode.NameOffset;

            subdirEntry.DataOffset = Nodes.IndexOf(subdirNode);

            subdirEntry.DataSize = 0x10;

            subdirEntry.Zero = 0;

            Entries.Add(subdirEntry);

            foreach (VirtualFolder dir in folder.Subdirs)
            {
                RecursiveDir(dir, subdirNode);
            }
        }

        void RecursiveFile(VirtualFolder folder)
        {
            foreach (FileData file in folder.Files)
            {
                Entries.Add(AddFileFileEntry(file));
            }

            AddPeriodEntries(0);

            foreach (VirtualFolder dir in folder.Subdirs)
            {
                RecursiveFile(dir);
            }
        }

        void Write(EndianBinaryWriter writer)
        {
            Header.Write(writer);

            Pad32(writer);

            foreach (Node node in Nodes)
            {
                node.Write(writer);
            }

            Pad32(writer);

            foreach (FileEntry entry in Entries)
            {
                entry.Write(writer);
            }

            Pad32(writer);

            writer.Write(StringTable.ToArray());

            Pad32(writer);

            writer.Write(Data.ToArray());
        }

        void Pad32(EndianBinaryWriter writer)
        {
            // Pad up to a 32 byte alignment Formula:
            // (x + (n-1)) & ~(n-1)
            long nextAligned = (writer.BaseStream.Length + 0x1F) & ~0x1F;

            long delta = nextAligned - writer.BaseStream.Length;
            writer.BaseStream.Position = writer.BaseStream.Length;
            writer.Write(new byte[delta]);

        }
    }
}
