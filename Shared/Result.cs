namespace Shared
{
    public class Result<S, E>
    {
        public readonly S? Success;
        public readonly E? Error;

        public bool IsSuccess { get; }
        public bool IsError { get; }

        public static Result<S, E> Ok(S success) => new Result<S, E>(success, default);
        public static Result<S, E> Err(E error) => new Result<S, E>(default, error);

        public Result(S? success, E? error) {
            Success = success; 
            Error = error;

            IsSuccess = !object.Equals(success, default(S));
            IsError = !object.Equals(error, default(E));
        }
    }
}
