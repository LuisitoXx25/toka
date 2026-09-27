namespace Toka.Domain.Orders;

/// <summary>Funding type of a card, resolved from its BIN. Interest-free installments require a credit card.</summary>
public enum CardType
{
    Unknown = 0,
    Credit = 1,
    Debit = 2,
}

public static class CardTypeExtensions
{
    /// <summary>Spanish label for user-facing text.</summary>
    public static string ToDisplayName(this CardType type) => type switch
    {
        CardType.Credit => "Crédito",
        CardType.Debit => "Débito",
        _ => "No identificado",
    };
}
