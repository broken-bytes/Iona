namespace Shared
{
    public class Result<S, E>(S? success, E? error)
    {
        public readonly S? Success = success;
        public readonly E? Error = error;

        public bool IsSuccess { get; } = !object.Equals(success, default(S));

        public bool IsError
        {
            get
            {
                if (error is null)
                {
                    return true;
                }

                if (error is Enum)
                {
                    return true;
                }
                
                return !Equals(error, default(E));
            }
        }

        public static Result<S, E> Ok(S success) => new(success, default);
        public static Result<S, E> Err(E error) => new(default, error);

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
