namespace ASB.Entitlements.Domain.Common;

/// <summary>
/// Represents the result of an operation with success/failure state
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Successful result cannot have an error");

        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Failed result must have an error message");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default!, false, error);
}

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
public class Result<T> : Result
{
    private readonly T _value;

    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot access Value of a failure result");

            return _value;
        }
    }

    protected internal Result(T value, bool isSuccess, string error)
        : base(isSuccess, error)
    {
        _value = value;
    }
}
