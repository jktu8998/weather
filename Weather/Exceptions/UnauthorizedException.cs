namespace Weather.Exceptions;

public class UnauthorizedException : DomainException
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    
    public UnauthorizedException(string message) : base(message) { }
}