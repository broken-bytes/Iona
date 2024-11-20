namespace Shared
{
    public class Result<S, E>(S? success, E? error)
    {
        public readonly S? Success = success;
        public readonly E? Error = error;

        public bool IsSuccess { get; } = !object.Equals(success, default(S));
        public bool IsError { get; } = !object.Equals(error, default(E));

        public static Result<S, E> Ok(S success) => new Result<S, E>(success, default);
        public static Result<S, E> Err(E error) => new Result<S, E>(default, error);

        public S Unwrapped()
        {
            if (IsSuccess)
            {
                return Success!;
            }

            throw new NullReferenceException("Unwrapping an result that is erroneous is not allowed.");
        }
    }
}
