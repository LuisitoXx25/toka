using Microsoft.Extensions.Options;
using Toka.Application.Payments;
using Toka.Domain.Orders;

namespace Toka.Infrastructure.Payments;

/// <summary>
/// Stand-in for the acquirer's BIN table. BINs listed in <see cref="SimulatorOptions.DebitBins"/> are debit;
/// any other card is treated as credit.
/// </summary>
public sealed class SimulatedCardBinLookup(IOptions<SimulatorOptions> options) : ICardBinLookup
{
    public CardType Lookup(string cardNumberOrBin)
    {
        if (cardNumberOrBin.Length < 6 || !cardNumberOrBin.AsSpan(0, 6).ContainsOnlyDigits()) return CardType.Unknown;
        var bin = cardNumberOrBin[..6];
        return options.Value.DebitBins.Contains(bin) ? CardType.Debit : CardType.Credit;
    }
}

internal static class SpanExtensions
{
    public static bool ContainsOnlyDigits(this ReadOnlySpan<char> span)
    {
        foreach (var c in span)
            if (!char.IsAsciiDigit(c)) return false;
        return true;
    }
}
