namespace PlanetoidGen.Contracts.Models.Reflection
{
    public class ValidationAttributeInfo
    {
        public string? Name { get; set; }

        public ValidationAttributePropertyInfo[]? PropertiesInfos { get; set; }
    }
}
