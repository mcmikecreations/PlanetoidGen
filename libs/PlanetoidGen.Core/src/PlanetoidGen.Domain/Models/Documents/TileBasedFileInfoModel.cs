using System;

namespace PlanetoidGen.Domain.Models.Documents
{
    public class TileBasedFileInfoModel
    {
        public string FileId { get; set; }
        public int PlanetoidId { get; set; }
        public short Z { get; set; }
        public long X { get; set; }
        public long Y { get; set; }
        public double[] Position { get; set; }
        public double[] Rotation { get; set; }
        public double[] Scale { get; set; }

        public TileBasedFileInfoModel(string fileId, int planetoidId, short z, long x, long y, double[] position, double[] rotation, double[] scale)
        {
            FileId = fileId;
            PlanetoidId = planetoidId;
            Z = z;
            X = x;
            Y = y;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public TileBasedFileInfoModel(string fileId, int planetoidId, short z, long x, long y)
            : this(fileId, planetoidId, z, x, y, Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double>())
        {
        }
    }
}
