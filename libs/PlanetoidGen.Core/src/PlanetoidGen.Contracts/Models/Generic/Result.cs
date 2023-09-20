using System;
using System.Text;

namespace PlanetoidGen.Contracts.Models.Generic
{
    public class Result<TData>
    {
        public bool Success { get; private set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TData Data { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ResponseMessage? ErrorMessage { get; private set; }

        public static Result<TData> CreateSuccess()
#pragma warning disable CS8601 // Possible null reference assignment.
            => new Result<TData>
            {
                Success = true,
                Data = default,
            };
#pragma warning restore CS8601 // Possible null reference assignment.

        public static Result<TData> CreateSuccess(TData data)
            => new Result<TData>
            {
                Success = true,
                Data = data,
            };

        public static Result<TData> Convert<TSource>(Result<TSource> result) where TSource : TData
        {
            return new Result<TData>
            {
                Success = result.Success,
                Data = result.Data,
                ErrorMessage = result.ErrorMessage,
            };
        }

        public static Result<TData> CreateFailure<TSource>(Result<TSource> result)
        {
            return new Result<TData>
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
            };
        }

        public static Result<TData> CreateFailure(Result result)
        {
            return new Result<TData>
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
            };
        }

        public static Result<TData> CreateFailure(string message, params string[] arguments)
        {
            return new Result<TData>
            {
                Success = false,
                ErrorMessage = new ResponseMessage(message, arguments),
            };
        }

        public static Result<TData> CreateFailure(Exception e)
        {
            return new Result<TData>
            {
                Success = false,
                ErrorMessage = new ResponseMessage(e.ToString(), new ResponseMessage(Environment.StackTrace)),
            };
        }

        public static Result<TData> CreateFailure(string message, Exception inner)
        {
            return new Result<TData>
            {
                Success = false,
                ErrorMessage = new ResponseMessage(message, CreateFailure(inner).ErrorMessage!),
            };
        }

        public static Result<TData> CreateFailure(string message, ResponseMessage inner)
        {
            return new Result<TData>
            {
                Success = false,
                ErrorMessage = new ResponseMessage(message, inner),
            };
        }

        public static Result<TData> CreateFailure(ResponseMessage errorMessage)
        {
            return new Result<TData>
            {
                Success = false,
                ErrorMessage = errorMessage,
            };
        }

        public override string ToString()
        {
            if (Success)
            {
                var builder = new StringBuilder();

                builder.Append(nameof(Success));
                builder.AppendLine(" (");
                builder.AppendLine(Data!.ToString());
                builder.AppendLine(")");

                return builder.ToString();
            }

            return ErrorMessage?.ToString() ?? nameof(Result);
        }
    }
}
