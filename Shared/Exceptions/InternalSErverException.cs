namespace Shared.Exceptions;

public class InternalServerException : Exception
{
	public InternalServerException(String message) : base(message)
	{
	}

	public InternalServerException(String message, String details)
		: base(message)
	{
		this.Details = details;
	}

	public String? Details { get; }
}
