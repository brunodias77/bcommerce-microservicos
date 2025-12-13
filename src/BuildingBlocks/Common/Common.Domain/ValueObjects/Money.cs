namespace Common.Domain.ValueObjects;

// src/BuildingBlocks/Common/Common.Domain/ValueObjects/Money.cs


/// <summary>
/// Value Object para representar valores monetários
/// </summary>
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Amount = Math.Round(amount, 2);
        Currency = currency.ToUpper();
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal multiplier)
    {
        return new Money(Amount * multiplier, Currency);
    }

    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException();

        return new Money(Amount / divisor, Currency);
    }

    public bool IsZero() => Amount == 0;

    public bool IsPositive() => Amount > 0;

    public bool IsNegative() => Amount < 0;

    public static Money Zero(string currency = "BRL") => new(0, currency);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:N2}";

    public static implicit operator decimal(Money money) => money.Amount;
}