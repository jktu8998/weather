namespace Weather.Exceptions;

public abstract class DomainException : Exception
{
    public abstract int StatusCode { get; }
    
    protected DomainException(string message) : base(message) { }
}