namespace Inventory_Management.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base() { }

    // The message is shown to the caller, so it says why, not who or what exists.
    public ForbiddenAccessException(string message) : base(message) { }
}
