using PlanetoidGen.Contracts.Models;
using PlanetoidGen.Contracts.Models.Agents;
using PlanetoidGen.Contracts.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SDA = System.ComponentModel.DataAnnotations;

namespace PlanetoidGen.BusinessLogic.Agents.Models.Agents
{
    public abstract class BaseAgentSettings<TData> : IAgentSettings<TData>
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        public JsonSerializerOptions GetJsonSerializerOptions()
        {
            return _jsonSerializerOptions;
        }

        public virtual ValueTask<Result<TData>> Deserialize(string value)
        {
            try
            {
                var options = JsonSerializer.Deserialize<TData>(value, _jsonSerializerOptions);

                return options == null
                    ? new ValueTask<Result<TData>>(Result<TData>.CreateFailure($"{nameof(value)} deserialized to null"))
                    : new ValueTask<Result<TData>>(Result<TData>.CreateSuccess(options));
            }
            catch (Exception ex)
            {
                return new ValueTask<Result<TData>>(Result<TData>.CreateFailure(ex));
            }
        }

        public virtual ValueTask<Result<string>> Serialize(TData value)
        {
            try
            {
                return new ValueTask<Result<string>>(Result<string>.CreateSuccess(
                    JsonSerializer.Serialize(value, _jsonSerializerOptions)));
            }
            catch (Exception ex)
            {
                return new ValueTask<Result<string>>(Result<string>.CreateFailure(ex));
            }
        }

        public virtual ValueTask<string> Serialize()
        {
            return new ValueTask<string>(JsonSerializer.Serialize(this, typeof(TData), _jsonSerializerOptions));
        }

        public virtual async ValueTask<Result<ValidationResult>> Validate(string settings)
        {
            var deserializationResult = await Deserialize(settings);

            if (!deserializationResult.Success)
            {
                return Result<ValidationResult>.CreateFailure(deserializationResult);
            }

            var value = deserializationResult.Data!;
            var results = new List<SDA.ValidationResult>();
            var context = new SDA.ValidationContext(value, null, null);
            var success = SDA.ValidationResult.Success;

            var isValid = SDA.Validator.TryValidateObject(
                value,
                context,
                results,
                validateAllProperties: true);

            return Result<ValidationResult>.CreateSuccess(isValid
                ? new ValidationResult()
                : new ValidationResult(results
                    .Where(x => x != success && x.ErrorMessage != null)
                    .Select(x => x.ErrorMessage!)
                    .ToList()));
        }
    }
}
