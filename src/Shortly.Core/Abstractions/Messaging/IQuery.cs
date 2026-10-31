using MediatR;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}