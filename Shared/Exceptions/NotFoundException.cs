namespace Shared.Exceptions;

public class NotFoundException : Exception
{
	public NotFoundException(String message)
		: base(message)
	{
	}
	public NotFoundException(String name, Object key)
				: base($"Entity \"{name}\" ({key}) was not found.")
	{
	}
}
