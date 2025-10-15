using System.ComponentModel.DataAnnotations.Schema;
using BuildingBlocks.Mediator;
using BuildingBlocks.Validations;

namespace BuildingBlocks.Domain;

public abstract class Entity
{
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("version")]
    public int Version { get; set; } = 1;
    
    private readonly List<INotification> _domainEvents = new();

    /// <summary>
    /// Coleção somente leitura dos domain events pendentes
    /// </summary>
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adiciona um domain event à coleção
    /// </summary>
    /// <param name="domainEvent">O domain event a ser adicionado</param>
    protected void AddDomainEvent(INotification domainEvent)
    {
        if (domainEvent != null)
            _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Remove um domain event específico da coleção
    /// </summary>
    /// <param name="domainEvent">O domain event a ser removido</param>
    protected void RemoveDomainEvent(INotification domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Limpa todos os domain events da coleção
    /// </summary>
    protected internal void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Valida a entidade e retorna um ValidationHandler com os erros encontrados
    /// </summary>
    /// <returns>ValidationHandler com os erros de validação</returns>
    public abstract ValidationHandler Validate();



}