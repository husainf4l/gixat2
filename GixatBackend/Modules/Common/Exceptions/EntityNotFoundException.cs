namespace GixatBackend.Modules.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested entity is not found
/// </summary>
internal sealed class EntityNotFoundException : Exception
{
    public string EntityType { get; }
    public object EntityId { get; }

    public EntityNotFoundException()
        : base("Entity not found")
    {
        EntityType = string.Empty;
        EntityId = string.Empty;
    }

    public EntityNotFoundException(string message)
        : base(message)
    {
        EntityType = string.Empty;
        EntityId = string.Empty;
    }

    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        EntityType = string.Empty;
        EntityId = string.Empty;
    }

    public EntityNotFoundException(string entityType, object entityId)
        : base($"{entityType} with ID '{entityId}' not found")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public EntityNotFoundException(string entityType, object entityId, string message)
        : base(message)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
/// Exception thrown when an entity is not found within the user's organization
/// </summary>
internal sealed class EntityNotFoundInOrganizationException : Exception
{
    public string EntityType { get; }
    public object EntityId { get; }
    public Guid OrganizationId { get; }

    public EntityNotFoundInOrganizationException()
        : base("Entity not found in organization")
    {
        EntityType = string.Empty;
        EntityId = string.Empty;
        OrganizationId = Guid.Empty;
    }

    public EntityNotFoundInOrganizationException(string message)
        : base(message)
    {
        EntityType = string.Empty;
        EntityId = string.Empty;
        OrganizationId = Guid.Empty;
    }

    public EntityNotFoundInOrganizationException(string message, Exception innerException)
        : base(message, innerException)
    {
        EntityType = string.Empty;
        EntityId = string.Empty;
        OrganizationId = Guid.Empty;
    }

    public EntityNotFoundInOrganizationException(string entityType, object entityId, Guid organizationId)
        : base($"{entityType} with ID '{entityId}' not found in organization {organizationId}")
    {
        EntityType = entityType;
        EntityId = entityId;
        OrganizationId = organizationId;
    }
}
