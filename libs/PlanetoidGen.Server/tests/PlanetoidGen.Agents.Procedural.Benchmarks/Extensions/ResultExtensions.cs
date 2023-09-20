using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Generic;

namespace PlanetoidGen.Agents.Procedural.Benchmarks.Extensions
{
    public static class ResultExtensions
    {
        public static void EnsureSuccess(this Result result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage!.ToString());
            }
        }

        public static void EnsureSuccess<T>(this Result<T> result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage!.ToString());
            }
        }
    }
}
