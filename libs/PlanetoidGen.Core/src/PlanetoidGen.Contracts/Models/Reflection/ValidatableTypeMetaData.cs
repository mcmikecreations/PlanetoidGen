using System.Collections.Generic;

namespace PlanetoidGen.Contracts.Models.Reflection
{
    public class ValidatableTypeMetaData
    {
        public string? Name { get; set; }

        public string? TypeName { get; set; }

        public string? Description { get; set; }

        public bool IsNullable { get; set; }

        public IReadOnlyList<ValidationAttributeInfo>? ValidationAttributes { get; set; }
    }
}
