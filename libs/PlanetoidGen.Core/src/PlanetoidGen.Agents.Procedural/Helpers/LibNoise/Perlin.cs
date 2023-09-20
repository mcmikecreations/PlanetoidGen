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

using PlanetoidGen.Agents.Procedural.Helpers.LibNoise.Helpers;
using System;

namespace PlanetoidGen.Agents.Procedural.Helpers.LibNoise
{
    public class Perlin
    {
        private const int PERLIN_MAX_OCTAVE = 30;

        private readonly double _frequency;
        private readonly double _lacunarity;
        private readonly NoiseQuality _noiseQuality;
        private readonly int _octaveCount;
        private readonly double _persistence;
        private readonly int _seed;

        /// <summary>
        /// Initialises an instance of <see cref="Perlin"/>.
        /// </summary>
        /// <param name="frequency">Number that determines at what distance to view the noisemap.</param>
        /// <param name="lacunarity">Number that determines how much detail is added or removed at each octave (adjusts frequency).</param>
        /// <param name="noiseQuality">Value that determines noise quality.</param>
        /// <param name="octaveCount">The number of levels of detail you want you perlin noise to have.</param>
        /// <param name="persistence">Number that determines how much each octave contributes to the overall shape (adjusts amplitude).</param>
        /// <param name="seed">A starting point for a sequence of pseudorandom numbers</param>
        public Perlin(
            double frequency = 1.0,
            double lacunarity = 2.0,
            NoiseQuality noiseQuality = NoiseQuality.QUALITY_STD,
            int octaveCount = 6,
            double persistence = 0.5,
            int seed = 0)
        {
            _frequency = frequency;
            _lacunarity = lacunarity;
            _noiseQuality = noiseQuality;
            _octaveCount = Math.Min(PERLIN_MAX_OCTAVE, octaveCount);
            _persistence = persistence;
            _seed = seed;
        }

        public double GetValue(double x, double y, double z)
        {
            var value = 0.0;
            var curPersistence = 1.0;
            double nx, ny, nz;
            int seed;

            x *= _frequency;
            y *= _frequency;
            z *= _frequency;

            for (var curOctave = 0; curOctave < _octaveCount; ++curOctave)
            {
                // Make sure that these floating-point values have the same range as a 32-
                // bit integer so that we can pass them to the coherent-noise functions.
                nx = NoiseUtils.MakeInt32Range(x);
                ny = NoiseUtils.MakeInt32Range(y);
                nz = NoiseUtils.MakeInt32Range(z);

                // Get the coherent-noise value from the input value and add it to the
                // final result.
                seed = (int)(_seed + curOctave & 0xffffffff);
                var signal = NoiseUtils.GradientCoherentNoise3D(nx, ny, nz, seed, _noiseQuality);
                value += signal * curPersistence;

                // Prepare the next octave.
                x *= _lacunarity;
                y *= _lacunarity;
                z *= _lacunarity;
                curPersistence *= _persistence;
            }

            return value;
        }
    }
}
