namespace GharCraft.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency = "INR")
{
    public static Money Zero(string currency = "INR") => new(0m, currency);

    public static Money FromINR(decimal amount) => new(amount, "INR");

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot operate on amounts with different currencies ({a.Currency} vs {b.Currency}).");
    }
}
