using System;

namespace PlanetoidGen.DataAccess.Helpers.Extensions
{
    public static class DataExtensions
    {
        public static TValue? GetNullableValue<TValue>(
            this System.Data.IDataReader record,
            string name) where TValue : struct
        {
            var result = record[name];
            if (result is null) return null;
            else if (result is DBNull) return null;
            return result as TValue?;
        }
    }
}
