using FluentAssertions;
using NUnit.Framework.Internal.Execution;
using Portfolio.Domain.Entities;

public class PortfolioTestUtils
{
    public static void EnsurePurchaseRecord(PurchaseRecord purchaseRecord, decimal expectedReceivedAmount, decimal expectedPricePerUnit, DateTime expectedDate)
    {
        purchaseRecord.Amount.Should().Be(expectedReceivedAmount);
        purchaseRecord.PricePerUnit.Should().Be(expectedPricePerUnit);
        purchaseRecord.PurchaseDate.Should().Be(expectedDate);
    }

}