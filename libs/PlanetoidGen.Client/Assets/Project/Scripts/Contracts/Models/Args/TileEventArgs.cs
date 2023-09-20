using PlanetoidGen.Domain.Models.Info;
using System;
using System.Collections.Generic;

namespace PlanetoidGen.Client.Contracts.Models.Args
{
    public class TileEventArgs : EventArgs
    {
        public IEnumerable<TileInfoModel> TileInfos { get; set; }
    }
}
