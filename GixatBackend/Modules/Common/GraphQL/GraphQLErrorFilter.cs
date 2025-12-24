using GixatBackend.Modules.Common.Exceptions;
using HotChocolate;

namespace GixatBackend.Modules.Common.GraphQL;

/// <summary>
/// GraphQL error filter to provide user-friendly error messages
/// </summary>
internal sealed class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        
        // Handle EntityNotFoundException with clear message
        if (error.Exception is EntityNotFoundException entityNotFound)
        {
            return ErrorBuilder.FromError(error)
                .SetMessage(entityNotFound.Message)
                .SetCode("ENTITY_NOT_FOUND")
                .SetExtension("entityType", entityNotFound.EntityType)
                .SetExtension("entityId", entityNotFound.EntityId.ToString())
                .SetException(null)
                .Build();
        }

        // Handle BusinessRuleViolationException
        if (error.Exception is BusinessRuleViolationException businessViolation)
        {
            return ErrorBuilder.FromError(error)
                .SetMessage(businessViolation.Message)
                .SetCode("BUSINESS_RULE_VIOLATION")
                .SetExtension("ruleName", businessViolation.RuleName)
                .SetException(null)
                .Build();
        }

        // Handle ValidationException
        if (error.Exception is ValidationException validationEx)
        {
            return ErrorBuilder.FromError(error)
                .SetMessage("Validation failed")
                .SetCode("VALIDATION_ERROR")
                .SetExtension("errors", validationEx.Errors)
                .SetException(null)
                .Build();
        }

        // Handle ExternalServiceException
        if (error.Exception is ExternalServiceException externalService)
        {
            return ErrorBuilder.FromError(error)
                .SetMessage($"External service '{externalService.ServiceName}' error: {externalService.Message}")
                .SetCode("EXTERNAL_SERVICE_ERROR")
                .SetExtension("serviceName", externalService.ServiceName)
                .SetExtension("statusCode", externalService.StatusCode)
                .SetException(null)
                .Build();
        }

        // Handle UnauthorizedAccessException
        if (error.Exception is UnauthorizedAccessException)
        {
            return ErrorBuilder.FromError(error)
                .SetMessage("You do not have permission to perform this operation")
                .SetCode("FORBIDDEN")
                .SetException(null)
                .Build();
        }

        // Return original error for other cases
        return error;
    }
}
