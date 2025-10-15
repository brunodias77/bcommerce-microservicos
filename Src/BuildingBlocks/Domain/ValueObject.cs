using BuildingBlocks.Validations;

namespace BuildingBlocks.Domain;

public abstract class ValueObject
{
    /// <summary>
    /// Valida o value object e retorna um ValidationHandler com os erros encontrados.
    /// Deve ser implementado pelas classes derivadas.
    /// </summary>
    /// <returns>ValidationHandler com os erros de validação</returns>
    public abstract ValidationHandler Validate();

    /// <summary>
    /// Valida o value object e lança exceção se houver erros
    /// </summary>
    /// <exception cref="ValidationException">Lançada quando há erros de validação</exception>
    public virtual void ValidateAndThrow()
    {
        var validation = Validate();
        validation.ThrowIfHasErrors();
    }
}