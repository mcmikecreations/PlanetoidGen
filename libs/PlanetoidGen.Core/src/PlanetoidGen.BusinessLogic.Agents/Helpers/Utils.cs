using System;

namespace PlanetoidGen.BusinessLogic.Agents.Helpers
{
    public static class Utils
    {
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static (double x, double y, double z) ToCartesian(double lat, double lon)
        {
            var r = Math.Cos(lat);

            return (r * Math.Cos(lon), r * Math.Sin(lon), Math.Sin(lat));
        }

        public static (double x, double y, double z) ToCartesianUnity(double lat, double lon)
        {
            var r = Math.Cos(lat);

            return (r * Math.Cos(lon), Math.Sin(lat), r * Math.Sin(lon));
        }

        public static (double lat, double lon) ToSpherical(double x, double y, double z)
        {
            var lat = Math.Asin(z);
            var lon = Math.Atan2(y, x);

            return (lat, lon);
        }

        public static (double lat, double lon) ToSphericalUnity(double x, double y, double z)
        {
            var lat = Math.Asin(y);
            var lon = Math.Atan2(z, x);

            return (lat, lon);
        }

        public static double Mod(double x, double m)
        {
            var r = x % m;
            return r < 0.0 ? r + m : r;
        }

        public static (byte r, byte g, byte b, byte a) EncodeNoiseToRGBA32(float noise)
        {
            var encoded = BitConverter.GetBytes(noise);

            return (encoded[0], encoded[1], encoded[2], encoded[3]);
        }

        public static float DecodeNoiseFromRGBA32(byte r, byte g, byte b, byte a)
        {
            return BitConverter.ToSingle(new[] { r, g, b, a });
        }
    }
}
