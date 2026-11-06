using FluentValidation;
using MediatR;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Exceptions;
using ValidationException = Shortly.Core.Exceptions.ValidationException;

namespace Shortly.Core.Abstractions.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next(cancellationToken);

        // One context per validator: a shared context would carry earlier validators' failures into later results.
        var validationErrors = validators
            .Select(validator => validator.Validate(new ValidationContext<TRequest>(request)))
            .Where(validationResult => validationResult.Errors.Any())
            .SelectMany(validationResult => validationResult.Errors)
            .Select(validationFailure => new ValidationError(
                validationFailure.PropertyName,
                validationFailure.ErrorMessage))
            .ToList();

        if (validationErrors.Any()) throw new ValidationException(validationErrors);

        return await next();
    }
}