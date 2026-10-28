using System.Diagnostics.CodeAnalysis;

namespace Shortly.Domain.Abstractions;

public class Result
{
    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException();
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Domain.Abstractions.Result Success() => new(true, Error.None);

    public static Domain.Abstractions.Result Failure(Error error) => new(false, error);

    public static Domain.Abstractions.Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Domain.Abstractions.Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static Domain.Abstractions.Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}

public class Result<TValue> : Domain.Abstractions.Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static implicit operator Domain.Abstractions.Result<TValue>(TValue? value) => Create(value);
}