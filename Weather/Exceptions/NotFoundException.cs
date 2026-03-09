namespace Weather.Exceptions;

public class NotFoundException : DomainException
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    
    public NotFoundException(string message) : base(message) { }
}