using PlanetoidGen.Contracts.Models.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace PlanetoidGen.BusinessLogic.Helpers
{
    public static class AttributeMetaDataDescriptor
    {
        public static IEnumerable<ValidatableTypeMetaData> GetValidatableTypeAttributesMetaData(this Type type)
        {
            return type
                .GetProperties()
                .Select(p => new ValidatableTypeMetaData
                {
                    Name = p.Name,
                    TypeName = GetTypeName(p.PropertyType),
                    Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    IsNullable = IsNullable(p),
                    ValidationAttributes = p.GetValidationAttributes().ToList() ?? new List<ValidationAttributeInfo>(),
                })
                .ToList();
        }

        private static IEnumerable<ValidationAttributeInfo> GetValidationAttributes(this PropertyInfo propertyInfo)
        {
            return propertyInfo.CustomAttributes
                .Where(x => typeof(ValidationAttribute).IsAssignableFrom(x.AttributeType))
                .Select(x =>
                {
                    var attribute = propertyInfo.GetCustomAttribute(x.AttributeType);

                    return new ValidationAttributeInfo
                    {
                        Name = x.AttributeType.Name,
                        PropertiesInfos = x.AttributeType
                            .GetProperties()
                            .Where(p => p.PropertyType.IsPrimitive || p.PropertyType.Equals(typeof(string)))
                            .Select(p =>
                            {
                                var value = p.GetValue(attribute);

                                return new ValidationAttributePropertyInfo
                                {
                                    Name = p.Name,
                                    Value = value != null ? JsonSerializer.Serialize(p.GetValue(attribute)) : null,
                                };
                            })
                            .ToArray(),
                    };
                });
        }

        private static bool IsNullable(PropertyInfo p)
        {
            return Nullable.GetUnderlyingType(p.PropertyType) != null;
        }

        private static string GetTypeName(Type type)
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType(type);

            return nullableUnderlyingType != null ? nullableUnderlyingType.Name : type.Name;
        }
    }
}
