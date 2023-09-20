using PlanetoidGen.Contracts.Models.Generic;
using System;

namespace PlanetoidGen.Contracts.Models
{
    public sealed class Result
    {
        public bool Success { get; private set; }
        public ResponseMessage? ErrorMessage { get; private set; }

        public static Result CreateSuccess()
            => new Result
            {
                Success = true
            };

        public static Result CreateFailure(string message, params string[] arguments)
        {
            return new Result
            {
                Success = false,
                ErrorMessage = new ResponseMessage(message, arguments)
            };
        }

        public static Result CreateFailure(Exception e)
        {
            return new Result
            {
                Success = false,
                ErrorMessage = new ResponseMessage(e.ToString())
            };
        }

        public static Result CreateFailure(string message, Exception inner)
        {
            return new Result
            {
                Success = false,
                ErrorMessage = new ResponseMessage(message, CreateFailure(inner).ErrorMessage!)
            };
        }

        public static Result CreateFailure(string message, ResponseMessage inner)
        {
            return new Result
            {
                Success = false,
                ErrorMessage = new ResponseMessage(message, inner)
            };
        }

        public static Result CreateFailure(ResponseMessage message)
        {
            return new Result
            {
                Success = false,
                ErrorMessage = message
            };
        }

        public static Result CreateFailure<TData>(Result<TData> result)
        {
            return new Result
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
            };
        }

        public static Result CreateFailure(Result result)
        {
            return new Result
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
            };
        }

        public static Result Convert<TSource>(Result<TSource> result)
        {
            return new Result
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
            };
        }

        public override string ToString()
        {
            return Success ? nameof(Success) : ErrorMessage?.ToString() ?? nameof(Result);
        }
    }
}
