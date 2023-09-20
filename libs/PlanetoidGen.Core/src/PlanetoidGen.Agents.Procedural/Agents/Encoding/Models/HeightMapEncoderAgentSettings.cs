using PlanetoidGen.BusinessLogic.Agents.Models.Agents;

namespace PlanetoidGen.Agents.Procedural.Agents.Encoding.Models
{
    public class HeightMapEncoderAgentSettings : BaseAgentSettings<HeightMapEncoderAgentSettings>
    {
        public float MaxAltitude { get; set; }

        public float MaxMaskAltitude { get; set; }
    }
}
