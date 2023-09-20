namespace PlanetoidGen.Contracts.Constants.StringMessages
{
    public static class JobStringMessages
    {
        private const string Prefix = "Job";

        public static readonly string JobAlreadyExists = $"{Prefix}_{nameof(JobAlreadyExists)}";
        public static readonly string TileNotExists = $"{Prefix}_{nameof(TileNotExists)}";
    }
}
