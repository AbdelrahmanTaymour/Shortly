namespace Shortly.Core.Exceptions;

public sealed class UniqueConstraintViolationException(string constraintName) : Exception
{
    public string ConstraintName { get; } = constraintName;
}