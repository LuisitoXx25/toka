using Toka.Domain.Orders;

namespace Toka.Application.Payments;

/// <summary>Resolves whether a card is credit or debit from its BIN (first 6 digits). Implemented by the acquirer simulator.</summary>
public interface ICardBinLookup
{
    CardType Lookup(string cardNumberOrBin);
}
