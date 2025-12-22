namespace GixatBackend.Modules.Common.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public sealed class BusinessRuleViolationException : Exception
{
    public string RuleName { get; }

    public BusinessRuleViolationException(string ruleName, string message)
        : base(message)
    {
        RuleName = ruleName;
    }

    public BusinessRuleViolationException(string ruleName, string message, Exception innerException)
        : base(message, innerException)
    {
        RuleName = ruleName;
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public sealed class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }
}
