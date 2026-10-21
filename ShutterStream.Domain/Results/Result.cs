namespace ShutterStream.Domain.Results
{
    /// <summary>
    /// Why a <see cref="Result"/> failed, so a controller can pick the right status code without
    /// having to inspect the error text.
    /// </summary>
    public enum ResultErrorType
    {
        None,
        BadRequest,
        NotFound,
        Conflict,
        Unauthorized
    }

    /// <summary>
    /// Outcome of an operation that can fail for an expected, user-facing reason (validation,
    /// a missing album, a job that is already running). Used instead of throwing, so expected
    /// failures are part of the method signature rather than control flow via exceptions.
    ///
    /// Error is always safe to show to the user. Genuinely exceptional situations (missing
    /// config, broken invariants, database failures) should still throw and become a 500.
    /// </summary>
    public class Result
    {
        public bool Success { get; }

        /// <summary>Safe, user-facing reason for the failure. Empty when Success is true.</summary>
        public string Error { get; }

        /// <summary>The kind of failure. Always None when Success is true.</summary>
        public ResultErrorType ErrorType { get; }

        protected Result(bool success, string error, ResultErrorType errorType)
        {
            Success = success;
            Error = error;
            ErrorType = errorType;
        }

        public static Result Ok() => new(true, "", ResultErrorType.None);

        /// <summary>A failure caused by invalid input or a rule that forbids the change. Maps to HTTP 400.</summary>
        public static Result Fail(string error) => new(false, error, ResultErrorType.BadRequest);

        /// <summary>A failure caused by the requested thing not existing. Maps to HTTP 404.</summary>
        public static Result NotFoundFail(string error) => new(false, error, ResultErrorType.NotFound);

        /// <summary>A failure caused by the current state clashing with the request. Maps to HTTP 409.</summary>
        public static Result ConflictFail(string error) => new(false, error, ResultErrorType.Conflict);

        /// <summary>A failure caused by missing or invalid credentials. Maps to HTTP 401.</summary>
        public static Result UnauthorizedFail(string error) => new(false, error, ResultErrorType.Unauthorized);

        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

        public static Result<T> Fail<T>(string error) => Result<T>.Fail(error);

        public static Result<T> NotFoundFail<T>(string error) => Result<T>.NotFoundFail(error);

        public static Result<T> ConflictFail<T>(string error) => Result<T>.ConflictFail(error);

        public static Result<T> UnauthorizedFail<T>(string error) => Result<T>.UnauthorizedFail(error);
    }

    /// <summary>
    /// A <see cref="Result"/> that carries a value when it succeeds.
    /// </summary>
    public class Result<T> : Result
    {
        private readonly T? _value;

        private Result(bool success, string error, T? value, ResultErrorType errorType) : base(success, error, errorType)
        {
            _value = value;
        }

        /// <summary>
        /// The produced value. Only meaningful when Success is true - check that first.
        /// </summary>
        public T Value => Success
            ? _value!
            : throw new InvalidOperationException("Cannot read Value from a failed Result. Check Success first.");

        public static Result<T> Ok(T value) => new(true, "", value, ResultErrorType.None);

        /// <summary>A failure caused by invalid input or a rule that forbids the change. Maps to HTTP 400.</summary>
        public static new Result<T> Fail(string error) => new(false, error, default, ResultErrorType.BadRequest);

        /// <summary>A failure caused by the requested thing not existing. Maps to HTTP 404.</summary>
        public static new Result<T> NotFoundFail(string error) => new(false, error, default, ResultErrorType.NotFound);

        /// <summary>A failure caused by the current state clashing with the request. Maps to HTTP 409.</summary>
        public static new Result<T> ConflictFail(string error) => new(false, error, default, ResultErrorType.Conflict);

        /// <summary>A failure caused by missing or invalid credentials. Maps to HTTP 401.</summary>
        public static new Result<T> UnauthorizedFail(string error) => new(false, error, default, ResultErrorType.Unauthorized);
    }
}
