using PlanetoidGen.Contracts.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanetoidGen.Agents.Standard.Extensions
{
    public static class ValidatorExtension
    {
        public static ValueTask<ValidationResult> Validate(this object value)
        {
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(value, null, null);
            var success = System.ComponentModel.DataAnnotations.ValidationResult.Success;

            return System.ComponentModel.DataAnnotations.Validator.TryValidateObject(value, context, results, validateAllProperties: true)
                ? new ValueTask<ValidationResult>(new ValidationResult())
                : new ValueTask<ValidationResult>(new ValidationResult(results
                    .Where(x => x != success && x.ErrorMessage != null)
                    .Select(x => x.ErrorMessage!)
                    .ToList()));
        }
    }
}
