namespace PlanetoidGen.Domain.Models.Generation
{
    public class GenerationLODModel
    {
        public int PlanetoidId { get; }

        public short LOD { get; }

        public short Z { get; }

        public GenerationLODModel(int planetoidId, short lod, short z)
        {
            PlanetoidId = planetoidId;
            LOD = lod;
            Z = z;
        }
    }
}
