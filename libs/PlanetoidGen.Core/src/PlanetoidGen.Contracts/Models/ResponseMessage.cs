using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetoidGen.Contracts.Models
{
    public class ResponseMessage
    {
        public string Message { get; }

        public string[] Arguments { get; }

        public ResponseMessage(string message, IEnumerable<string>? arguments = null)
        {
            Message = message;
            Arguments = arguments?.ToArray() ?? new string[0];
        }

        public ResponseMessage(string message, ResponseMessage inner)
        {
            Message = message;
            Arguments = new string[] { inner.Message }.Concat(inner.Arguments).ToArray();
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            if (Arguments.Length == 0)
            {
                result.AppendLine(Message);
                return result.ToString();
            }
            else
            {
                result.Append(Message);
                result.AppendLine(" (");
                foreach (var arg in Arguments)
                {
                    result.Append('\t');
                    result.AppendLine(arg);
                }

                result.AppendLine(")");
                return result.ToString();
            }
        }
    }
}
