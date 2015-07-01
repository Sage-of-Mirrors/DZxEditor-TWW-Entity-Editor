using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DZxEditor
{
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
}
