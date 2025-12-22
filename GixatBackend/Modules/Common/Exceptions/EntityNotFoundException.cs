namespace GixatBackend.Modules.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested entity is not found
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    public string EntityType { get; }
    public object EntityId { get; }

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
public sealed class EntityNotFoundInOrganizationException : Exception
{
    public string EntityType { get; }
    public object EntityId { get; }
    public Guid OrganizationId { get; }

    public EntityNotFoundInOrganizationException(string entityType, object entityId, Guid organizationId)
        : base($"{entityType} with ID '{entityId}' not found in organization {organizationId}")
    {
        EntityType = entityType;
        EntityId = entityId;
        OrganizationId = organizationId;
    }
}
