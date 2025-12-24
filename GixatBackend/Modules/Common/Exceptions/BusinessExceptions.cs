namespace GixatBackend.Modules.Common.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
internal sealed class BusinessRuleViolationException : Exception
{
    public string RuleName { get; }

    public BusinessRuleViolationException()
        : base("A business rule was violated")
    {
        RuleName = string.Empty;
    }

    public BusinessRuleViolationException(string message)
        : base(message)
    {
        RuleName = string.Empty;
    }

    public BusinessRuleViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
        RuleName = string.Empty;
    }

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
internal sealed class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation errors occurred")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }

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
