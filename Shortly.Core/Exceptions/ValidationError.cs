namespace Shortly.Core.Exceptions;

public sealed record ValidationError(string PropertyName, string ErrorMessage);