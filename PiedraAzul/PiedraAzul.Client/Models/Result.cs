namespace PiedraAzul.Client.Models;

public record ErrorResult(
    string Message,
    string Type,
    bool RequiresLogout = false,
    object? Metadata = null
);

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public ErrorResult? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(ErrorResult error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(ErrorResult error) => new(error);
}
