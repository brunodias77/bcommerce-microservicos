using Common.Domain.Events;

namespace Common.Domain.Entities;

/// <summary>
/// Classe base para todas as entidades do domínio
/// </summary>
public abstract class Entity
{
    private int? _requestedHashCode;
    private List<IDomainEvent>? _domainEvents;

    public virtual Guid Id { get; protected set; }

    /// <summary>
    /// Eventos de domínio pendentes
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly() ?? new List<IDomainEvent>().AsReadOnly();

    /// <summary>
    /// Adiciona um evento de domínio
    /// </summary>
    public void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents ??= new List<IDomainEvent>();
        _domainEvents.Add(eventItem);
    }

    /// <summary>
    /// Remove um evento de domínio
    /// </summary>
    public void RemoveDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents?.Remove(eventItem);
    }

    /// <summary>
    /// Limpa todos os eventos de domínio
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    /// <summary>
    /// Verifica se é uma entidade transitória (ainda não persistida)
    /// </summary>
    public bool IsTransient()
    {
        return Id == default;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity entity)
            return false;

        if (ReferenceEquals(this, entity))
            return true;

        if (GetType() != entity.GetType())
            return false;

        if (entity.IsTransient() || IsTransient())
            return false;

        return entity.Id == Id;
    }

    public override int GetHashCode()
    {
        if (IsTransient())
            return base.GetHashCode();

        _requestedHashCode ??= Id.GetHashCode() ^ 31;

        return _requestedHashCode.Value;
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return left?.Equals(right) ?? Equals(right, null);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}