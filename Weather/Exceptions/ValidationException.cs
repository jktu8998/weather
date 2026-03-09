namespace Weather.Exceptions;

public class ValidationException : DomainException
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    
    public ValidationException(string message) : base(message) { }
}