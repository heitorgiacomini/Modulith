using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Shared.Contracts.CQRS;

namespace Shared.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    (IEnumerable<IValidator<TRequest>> _validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
  {
    // Validate the request
    ValidationContext<TRequest> context = new(request);

    ValidationResult[] validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

    List<ValidationFailure> failures = validationResults
        .Where(f => f.Errors.Any())
        .SelectMany(r => r.Errors)
        .ToList();

    return failures.Any() ? throw new ValidationException(failures) : await next();
  }
}
