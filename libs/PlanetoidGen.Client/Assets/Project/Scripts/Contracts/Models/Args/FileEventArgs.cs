using PlanetoidGen.Contracts.Models.Documents;
using System;

namespace PlanetoidGen.Client.Contracts.Models.Args
{
    public class FileEventArgs : EventArgs
    {
        public FileModel File { get; set; }
    }
}
