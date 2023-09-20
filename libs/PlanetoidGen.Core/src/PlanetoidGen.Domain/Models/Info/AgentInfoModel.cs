namespace PlanetoidGen.Domain.Models.Info
{
    public class AgentInfoModel
    {
        public int PlanetoidId { get; }

        /// <summary>
        /// 0-based index.
        /// </summary>
        public int IndexId { get; }

        public string Title { get; }

        public string Settings { get; }

        /// <summary>
        /// Defines if the related agent job can be executed if this job is last in agent list
        /// for given <see cref="PlanetoidId"/>.
        /// </summary>
        public bool ShouldRerunIfLast { get; }

        public AgentInfoModel(int planetoidId, int indexId, string title, string settings, bool shouldRerunIfLast)
        {
            PlanetoidId = planetoidId;
            IndexId = indexId;
            Title = title;
            Settings = settings;
            ShouldRerunIfLast = shouldRerunIfLast;
        }

        public override string ToString()
        {
            return $"P={PlanetoidId}, I={IndexId}, T={Title}, SR={ShouldRerunIfLast}";
        }
    }
}
