using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanetoidGen.Contracts.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class ValidationResult
    {
        private List<string> _errors;

        /// <summary>
        /// 
        /// </summary>
        public ValidationResult()
        {
            _errors = new List<string>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errors"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ValidationResult(List<string> errors)
        {
            if (errors is null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            _errors = errors.Where(e => e != null).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// 
        /// </summary>
        public List<string> Errors
        {
            get => _errors;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _errors = value.Where(e => e != null).ToList();
            }
        }

        /// <summary>
        /// Generates a string representation of the error messages separated by new lines.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(Environment.NewLine);
        }

        /// <summary>
        /// Generates a string representation of the error messages separated by the specified character.
        /// </summary>
        /// <param name="separator">The character to separate the error messages.</param>
        /// <returns></returns>
        public string ToString(string separator)
        {
            return string.Join(separator, _errors);
        }
    }
}
