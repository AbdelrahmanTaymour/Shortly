namespace Shortly.Core.Common;

public enum ResultStatus
{
    Ok,
    Created,
    NoContent,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    ValidationError,
    Error
}

public class Result
{
    public bool IsSuccess { get; init; }
    public ResultStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    public static Result Success(ResultStatus status = ResultStatus.Ok)
    {
        return new Result { IsSuccess = true, Status = status };
    }

    public static Result Failure(string msg, string? code = null, ResultStatus status = ResultStatus.BadRequest)
    {
        return new Result { IsSuccess = false, ErrorMessage = msg, ErrorCode = code, Status = status };
    }
}

public sealed class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Success(T data, ResultStatus status = ResultStatus.Ok)
    {
        return new Result<T> { IsSuccess = true, Data = data, Status = status };
    }

    public new static Result<T> Failure(string msg, string? code = null, ResultStatus status = ResultStatus.BadRequest)
    {
        return new Result<T> { IsSuccess = false, ErrorMessage = msg, ErrorCode = code, Status = status };
    }

    // Implicit conversion for cleaner service code
    public static implicit operator Result<T>(T data)
    {
        return Success(data);
    }
}