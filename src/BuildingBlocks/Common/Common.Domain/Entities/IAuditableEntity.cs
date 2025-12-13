namespace Common.Domain.Entities;

/// <summary>
/// Interface para entidades auditáveis
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// ID do usuário que criou a entidade
    /// </summary>
    Guid? CreatedBy { get; }

    /// <summary>
    /// Data de criação
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// ID do usuário que fez a última modificação
    /// </summary>
    Guid? UpdatedBy { get; }

    /// <summary>
    /// Data da última modificação
    /// </summary>
    DateTime UpdatedAt { get; }

    /// <summary>
    /// ID do usuário que excluiu a entidade
    /// </summary>
    Guid? DeletedBy { get; }

    /// <summary>
    /// Data de exclusão (soft delete)
    /// </summary>
    DateTime? DeletedAt { get; }

    /// <summary>
    /// Endereço IP da última operação
    /// </summary>
    string? LastIpAddress { get; }

    void SetCreatedBy(Guid userId, string? ipAddress = null);
    void SetUpdatedBy(Guid userId, string? ipAddress = null);
    void SetDeletedBy(Guid userId, string? ipAddress = null);
}

/// <summary>
/// Implementação base para entidades auditáveis
/// </summary>
public abstract class AuditableEntity : Entity, IAuditableEntity
{
    public Guid? CreatedBy { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public string? LastIpAddress { get; protected set; }

    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCreatedBy(Guid userId, string? ipAddress = null)
    {
        CreatedBy = userId;
        LastIpAddress = ipAddress;
    }

    public void SetUpdatedBy(Guid userId, string? ipAddress = null)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
        LastIpAddress = ipAddress;
    }

    public virtual void SetDeletedBy(Guid userId, string? ipAddress = null)
    {
        DeletedBy = userId;
        DeletedAt = DateTime.UtcNow;
        LastIpAddress = ipAddress;
    }
}