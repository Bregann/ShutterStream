using Microsoft.AspNetCore.Mvc;
using ShutterStream.Domain.Results;

namespace ShutterStream.Core.Extensions
{
    /// <summary>
    /// Turns a service <see cref="Result"/> into an HTTP response, so controllers do not each repeat
    /// the same mapping.
    ///
    /// When a new kind of failure is added to <see cref="ResultErrorType"/>, map it here and every
    /// endpoint picks it up.
    /// </summary>
    public static class ResultExtensions
    {
        private static readonly Dictionary<ResultErrorType, (int StatusCode, string Title)> _errorMap = new()
        {
            { ResultErrorType.BadRequest, (StatusCodes.Status400BadRequest, "Bad Request") },
            { ResultErrorType.NotFound, (StatusCodes.Status404NotFound, "Not Found") },
            { ResultErrorType.Conflict, (StatusCodes.Status409Conflict, "Conflict") },
            { ResultErrorType.Unauthorized, (StatusCodes.Status401Unauthorized, "Unauthorized") },
        };

        /// <summary>
        /// Maps a failed result to its status code, with a ProblemDetails body carrying the error as
        /// its detail (the web client reads it from there). Only call this once the result is known
        /// to have failed - there is no single correct success response, so each action returns its own.
        /// </summary>
        /// <exception cref="InvalidOperationException">The result succeeded.</exception>
        public static ActionResult ToErrorResponse(this Result result)
        {
            if (result.Success)
            {
                throw new InvalidOperationException("ToErrorResponse is only for failed results. Check Success first and return the action's own success response.");
            }

            var mapped = _errorMap[result.ErrorType];

            return new ObjectResult(new ProblemDetails
            {
                Status = mapped.StatusCode,
                Title = mapped.Title,
                Detail = result.Error
            })
            {
                StatusCode = mapped.StatusCode
            };
        }

        /// <summary>
        /// Maps a result to an empty 200 on success, or its failure status. For actions with no
        /// body to return.
        /// </summary>
        public static ActionResult ToActionResult(this Result result)
        {
            return result.Success ? new OkResult() : result.ToErrorResponse();
        }

        /// <summary>
        /// Maps a result to a 200 carrying its value, or its failure status. Reading
        /// <see cref="Result{T}.Value"/> throws on a failed result, so this keeps that check in one
        /// place rather than in every action.
        /// </summary>
        public static ActionResult ToActionResult<T>(this Result<T> result)
        {
            return result.Success ? new OkObjectResult(result.Value) : result.ToErrorResponse();
        }
    }
}
