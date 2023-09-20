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

namespace PlanetoidGen.Agents.Procedural.Helpers.LibNoise
{
    public enum NoiseQuality
    {
        QUALITY_FAST = 0,
        QUALITY_STD = 1,
        QUALITY_BEST = 2
    };
}
