namespace Common.Domain.Entities;

/// <summary>
/// Classe base para Aggregate Roots no DDD
/// </summary>
public abstract class AggregateRoot : Entity, IAggregateRoot
{
    /// <summary>
    /// Versão para Optimistic Locking
    /// </summary>
    public int Version { get; protected set; } = 1;

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime UpdatedAt { get; protected set; }

    /// <summary>
    /// Data de exclusão lógica (soft delete)
    /// </summary>
    public DateTime? DeletedAt { get; protected set; }

    /// <summary>
    /// Verifica se a entidade foi excluída logicamente
    /// </summary>
    public bool IsDeleted => DeletedAt.HasValue;

    protected AggregateRoot()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca a entidade como excluída (soft delete)
    /// </summary>
    public virtual void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Entidade já está excluída");

        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restaura uma entidade excluída
    /// </summary>
    public virtual void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("Entidade não está excluída");

        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Incrementa a versão (chamado pelo trigger ou EF)
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Interface marcadora para Aggregate Roots
/// </summary>
public interface IAggregateRoot
{
    int Version { get; }
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    DateTime? DeletedAt { get; }
    bool IsDeleted { get; }
    void Delete();
}
