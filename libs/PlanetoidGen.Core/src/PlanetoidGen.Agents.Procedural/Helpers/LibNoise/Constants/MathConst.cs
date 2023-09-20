/*
 * This file is part of libnoise-dotnet.
 * libnoise-dotnet is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * libnoise-dotnet is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with libnoise-dotnet.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * Simplex Noise in 2D, 3D and 4D. Based on the example code of this paper:
 * http://staffwww.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf
 * 
 * From Stefan Gustavson, Linkping University, Sweden (stegu at itn dot liu dot se)
 * From Karsten Schmidt (slight optimizations & restructuring)
 */

namespace PlanetoidGen.Agents.Procedural.Helpers.LibNoise.Constants
{
    public static class MathConst
    {
        public static double PI = 3.1415926535897932385;

        /// Square root of 2.
        public static double SQRT_2 = 1.4142135623730950488;

        /// Square root of 3.
        public static double SQRT_3 = 1.7320508075688772935;

        /// Converts an angle from degrees to radians.
        public static double DEG_TO_RAD = PI / 180.0;

        /// Converts an angle from radians to degrees.
        public static double RAD_TO_DEG = 1.0 / DEG_TO_RAD;
    }
}
