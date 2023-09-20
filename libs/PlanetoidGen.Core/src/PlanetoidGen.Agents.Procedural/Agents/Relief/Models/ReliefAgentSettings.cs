using PlanetoidGen.BusinessLogic.Agents.Models.Agents;

namespace PlanetoidGen.Agents.Procedural.Agents.Relief.Models
{
    public class ReliefAgentSettings : BaseAgentSettings<ReliefAgentSettings>
    {
        public int TileSizeInPixels { get; set; }

        public float MaxMaskAltitude { get; set; }

        public float MaskEdgeThresholdNegativePercentage { get; set; }

        public float MaskEdgeThresholdPositivePercentage { get; set; }

        public float MaxMountainAltittude { get; set; }

        public float MinMountainThreshold { get; set; }

        public float MaxHillAltittude { get; set; }

        public float MinHillThreshold { get; set; }

        public float MinShorelineAltitude { get; set; }

        public float MaxShorelineAltitude { get; set; }

        public int GaussianKernelSize { get; set; }
    }
}
