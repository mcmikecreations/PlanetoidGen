using System;

namespace PlanetoidGen.Agents.Standard.Helpers
{
    public static class MathHelpers
    {
        /// <summary>
        /// Calculate distance between coordinates on a sphere.
        /// </summary>
        /// <param name="lat1">Latitude of first coordinate, radians.</param>
        /// <param name="lon1">Longtitude of first coordinate, radians.</param>
        /// <param name="lat2">Latitude of second coordinate, radians.</param>
        /// <param name="lon2">Longtitude of second coordinate, radians.</param>
        /// <param name="radius">Sphere radius, meters/units.</param>
        /// <returns>Distance between points on a sphere in <paramref name="radius"/> units.</returns>
        public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2, double radius)
        {
            // https://www.omnicalculator.com/other/latitude-longitude-distance#the-haversine-formula-or-haversine-distance
            // d = 2R⋅sin⁻¹(√[sin²((θ₂ - θ₁)/2) + cosθ₁⋅cosθ₂⋅sin²((φ₂ - φ₁)/2)])
            // θ₁, φ₁ – First point latitude and longitude coordinates;
            // θ₂, φ₂ – Second point latitude and longitude coordinates;
            // R – Earth's radius (R = 6371 km); and
            // d – Distance between them along Earth's surface.
            var latDiff = Math.Pow(Math.Sin((lat2 - lat1) * 0.5), 2.0);
            var lonDiff = Math.Pow(Math.Sin((lon2 - lon1) * 0.5), 2.0);
            var coordMult = Math.Cos(lat1) * Math.Cos(lat2) * lonDiff;
            var rootRes = Math.Sqrt(latDiff + coordMult);
            return 2.0 * radius * Math.Asin(rootRes);
        }

        public static int Modulo(int x, int m)
        {
            var r = x % m;
            return r < 0 ? r + m : r;
        }

        public static int Lcm(int a, int b)
        {
            return a / Gfc(a, b) * b;
        }

        static int Gfc(int a, int b)
        {
            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        public const float Sqrt2 = 1.41421356237f;
    }
}
