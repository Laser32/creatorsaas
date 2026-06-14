namespace CreatorSaaS.Core.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }
    public DomainException(string code, string message) : base(message) => Code = code;
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, Guid id) 
        : base("NOT_FOUND", $"{entity} with id '{id}' was not found.") { }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException() 
        : base("FORBIDDEN", "You do not have permission to perform this action.") { }
}

public class QuotaExceededException : DomainException
{
    public QuotaExceededException(string plan, int quota) 
        : base("QUOTA_EXCEEDED", $"Monthly video quota ({quota}) for plan '{plan}' has been reached.") { }
}

public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }
    public ValidationException(IDictionary<string, string[]> errors) 
        : base("VALIDATION_ERROR", "One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public class ExternalServiceException : DomainException
{
    public string Service { get; }
    public ExternalServiceException(string service, string message) 
        : base("EXTERNAL_SERVICE_ERROR", $"[{service}] {message}") 
    {
        Service = service;
    }
}
