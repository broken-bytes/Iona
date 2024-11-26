namespace Shared
{
    public class Result<S, E>
    {
        public readonly S? Success;
        public readonly E? Error;
        
        private readonly bool _isError;

        public bool IsSuccess => !_isError;

        public bool IsError => _isError;

        public static Result<S, E> Ok(S success) => new(success);
        public static Result<S, E> Err(E error) => new(error);

        private Result(S? success)
        {
            Success = success;
            _isError = false;
            Error = default;
        }

        private Result(E? error)
        {
            Error = error;
            _isError = true;
            Success = default;
        }

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
