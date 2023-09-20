using System;

namespace PlanetoidGen.Domain.Models.Info
{
    public class DataTypeInfoModel : IEquatable<DataTypeInfoModel>
    {
        public string Title { get; }

        public bool IsRaster { get; }

        public DataTypeInfoModel(string title, bool isRaster)
        {
            Title = title;
            IsRaster = isRaster;
        }

        public override bool Equals(object obj)
        {
            return obj is DataTypeInfoModel item && Equals(item);
        }

        public override int GetHashCode()
        {
            return Title.GetHashCode();
        }

        public bool Equals(DataTypeInfoModel other)
        {
            return Title == other.Title && IsRaster == other.IsRaster;
        }
    }
}
