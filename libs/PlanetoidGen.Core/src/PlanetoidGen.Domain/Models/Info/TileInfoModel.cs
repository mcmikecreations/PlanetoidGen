using System;

namespace PlanetoidGen.Domain.Models.Info
{
    public class TileInfoModel
    {
        public int PlanetoidId { get; }

        /// <summary>
        /// Zoom level, usually 0-30.
        /// </summary>
        public short Z { get; }

        public long X { get; }

        public long Y { get; }

        /// <summary>
        /// DB value for last agent that processed this tile.
        /// <see cref="LastAgent"/> value is <see langword="null"/> if tile
        /// has not been processed by any agents.
        /// </summary>
        public int? LastAgent { get; }

        /// <summary>
        /// Indexed value for last agent that processed this tile,
        /// which could be used during agent execution. If <see cref="LastAgent"/>
        /// is <see langword="null"/> then we can assume that the indexed value will be -1
        /// as <see cref="AgentInfoModel.IndexId"/> is 0-based index.
        /// </summary>
        public int LastIndexedAgent => LastAgent ?? -1;

        public string Id { get; }

        public DateTimeOffset CreatedDate { get; }

        public DateTimeOffset? ModifiedDate { get; }

        public TileInfoModel(
            int planetoidId,
            short z,
            long x,
            long y,
            int? lastAgent,
            string id,
            DateTimeOffset createdDate,
            DateTimeOffset? modifiedDate)
        {
            PlanetoidId = planetoidId;
            Z = z;
            X = x;
            Y = y;
            LastAgent = lastAgent;
            Id = id;
            CreatedDate = createdDate;
            ModifiedDate = modifiedDate;
        }

        public override string ToString()
        {
            return $"P={PlanetoidId}, Z={Z}, X={X}, Y={Y}, LA={LastAgent}, Id={Id}, CreatedDate={CreatedDate}, ModifiedDate={ModifiedDate}";
        }
    }
}
