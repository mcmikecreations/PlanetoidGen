namespace PlanetoidGen.Domain.Models.Info
{
    public class PlanetoidInfoModel
    {
        public int Id { get; }

        public string Title { get; }

        public long Seed { get; }

        public double Radius { get; }

        public PlanetoidInfoModel(int id, string title, long seed, double radius)
        {
            Id = id;
            Title = title;
            Seed = seed;
            Radius = radius;
        }

        public override string ToString()
        {
            return $"PlanetoidInfoModel(Id={Id}, Title={Title})";
        }
    }
}
