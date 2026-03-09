namespace Weather.Exceptions;

public class ConflictException : DomainException
{
    public override int StatusCode => StatusCodes.Status409Conflict;

    public ConflictException(string message) : base(message) {}
}