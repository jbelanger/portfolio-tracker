using System.Text;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Transactions.Importers.Csv.Coinbase;

namespace Portfolio.Transactions.Importers.Tests.Csv.Coinbase;

public class CoinbaseCsvParserTests
{
    private const string ValidCsvHeader = @"ID,Timestamp,Transaction Type,Asset,Quantity Transacted,Price Currency,Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes";

    private StreamReader CreateStreamReader(string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new StreamReader(stream);
    }

    [Fact]
    public void Create_WithValidCsv_ShouldSucceed()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";
        
        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithInvalidCsv_ShouldFail()
    {
        // Arrange
        var csv = "Invalid,CSV,Content";
        
        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    #region Header Validation

    [Fact]
    public void Create_WithUserLineBeforeHeader_ShouldSucceed()
    {
        // Arrange
        var csv = @"Transactions
User,John Smith,034e1614-b140-52cd-8c3b-ddd98f432ffa
ID,Timestamp,Transaction Type,Asset,Quantity Transacted,Price Currency,Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithOnlyUserLineAndNoHeader_ShouldFail()
    {
        // Arrange
        var csv = @"Transactions
User,John Smith,034e1614-b140-52cd-8c3b-ddd98f432ffa";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("The expected header was not found in the CSV file.");
    }

    [Fact]
    public void Create_WithExtraLinesAndUserLineBeforeHeader_ShouldSucceed()
    {
        // Arrange
        var csv = @"
Transactions
User,John Smith,034e1614-b140-52cd-8c3b-ddd98f432ffa

ID,Timestamp,Transaction Type,Asset,Quantity Transacted,Price Currency,Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithUserLineButInvalidHeader_ShouldFail()
    {
        // Arrange
        var csv = @"Transactions
User,John Smith,034e1614-b140-52cd-8c3b-ddd98f432ffa
ID,Timestamp,Transaction,Asset,Amount"; // Invalid header

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("The expected header was not found in the CSV file.");
    }

    [Fact]
    public void Create_WithHeaderMatchingExactly_ShouldSucceed()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithHeaderHavingExtraWhitespace_ShouldSucceed()
    {
        // Arrange
        var csv = $@"  {ValidCsvHeader}  
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithHeaderInDifferentCasing_ShouldSucceed()
    {
        // Arrange
        var csv = $@"id,timestamp,transaction type,asset,quantity transacted,price currency,price at transaction,subtotal,total (inclusive of fees and/or spread),fees and/or spread,notes
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMissingHeaderFields_ShouldFail()
    {
        // Arrange
        var csv = $@"ID,Timestamp,Transaction Type,Asset,Quantity Transacted
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("The expected header was not found in the CSV file.");
    }

    [Fact]
    public void Create_WithExtraHeaderFields_ShouldSucceed()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader},ExtraField
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,ExtraValue";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNoHeader_ShouldFail()
    {
        // Arrange
        var csv = @"664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("The expected header was not found in the CSV file.");
    }

    [Fact]
    public void Create_WithBlankLinesBeforeHeader_ShouldSucceed()
    {
        // Arrange
        var csv = $@"

{ValidCsvHeader}
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";

        // Act
        var result = CoinbaseCsvParser.Create(CreateStreamReader(csv));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }


    #endregion

    [Fact]
    public void ExtractTransactions_WithDeposit_ShouldCreateDepositTransaction()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,";
        var parser = CoinbaseCsvParser.Create(CreateStreamReader(csv)).Value;

        // Act
        var transactions = parser.ExtractTransactions().ToList();

        // Assert
        transactions.Should().HaveCount(1);
        var transaction = transactions[0] as FinancialTransaction;
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Deposit);
        transaction.ReceivedAmount.Amount.Should().Be(728.31m);
        transaction.ReceivedAmount.CurrencyCode.Should().Be("USD");
        transaction.FeeAmount.Amount.Should().Be(0m);
        transaction.FeeAmount.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void ExtractTransactions_WithWithdrawal_ShouldCreateWithdrawTransaction()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
664d24e5dc2198f4ddd2a6d7,2024-05-21 22:49:09 UTC,Withdrawal,CAD,500,USD,1,366.58,366.58,0,";
        var parser = CoinbaseCsvParser.Create(CreateStreamReader(csv)).Value;

        // Act
        var transactions = parser.ExtractTransactions().ToList();

        // Assert
        transactions.Should().HaveCount(1);
        var transaction = transactions[0] as FinancialTransaction;
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Withdrawal);
        transaction.SentAmount.Amount.Should().Be(366.58m);
        transaction.SentAmount.CurrencyCode.Should().Be("USD");
        transaction.FeeAmount.Amount.Should().Be(0m);
        transaction.FeeAmount.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void ExtractTransactions_WithBuyTrade_ShouldCreateTradeTransaction()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
663cec29e8ccfa4c2ddc099e,2024-05-09 15:30:49 UTC,Buy,DESO,16.760732234,USD,29.3937038741430442,359.96,365.33,5.362992009321613246,Bought 16.760732234 DESO for 365.33 USD";
        var parser = CoinbaseCsvParser.Create(CreateStreamReader(csv)).Value;

        // Act
        var transactions = parser.ExtractTransactions().ToList();

        // Assert
        transactions.Should().HaveCount(1);
        var transaction = transactions[0] as FinancialTransaction;
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Trade);
        transaction.ReceivedAmount.Amount.Should().Be(16.760732234m);
        transaction.ReceivedAmount.CurrencyCode.Should().Be("DESO");
        transaction.SentAmount.Amount.Should().Be(359.96m);
        transaction.SentAmount.CurrencyCode.Should().Be("USD");
        transaction.FeeAmount.Amount.Should().Be(5.362992009321613246m);
        transaction.FeeAmount.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void ExtractTransactions_WithAdvancedTradeSell_ShouldCreateTradeTransaction()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
664d295471a6a0f1cdd5ee46,2024-05-21 23:08:04 UTC,Advance Trade Sell,DESO,16.735,USD,19.01,318.13,316.22,-1.9087941,Sold 16.735 DESO for 316.22 USD on DESO-USDC";
        var parser = CoinbaseCsvParser.Create(CreateStreamReader(csv)).Value;

        // Act
        var transactions = parser.ExtractTransactions().ToList();

        // Assert
        transactions.Should().HaveCount(1);
        var transaction = transactions[0] as FinancialTransaction;
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Trade);
        transaction.ReceivedAmount.Amount.Should().Be(318.13m);
        transaction.ReceivedAmount.CurrencyCode.Should().Be("USD");
        transaction.SentAmount.Amount.Should().Be(16.735m);
        transaction.SentAmount.CurrencyCode.Should().Be("DESO");
        transaction.FeeAmount.Amount.Should().Be(1.9087941m);
        transaction.FeeAmount.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void ExtractTransactions_WithSend_ShouldCreateWithdrawTransaction()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Send,BTC,0.015765,USD,63289.295,997.76,997.76,0,Sent 0.015765 BTCs";
        var parser = CoinbaseCsvParser.Create(CreateStreamReader(csv)).Value;

        // Act
        var transactions = parser.ExtractTransactions().ToList();

        // Assert
        transactions.Should().HaveCount(1);
        var transaction = transactions[0] as FinancialTransaction;
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Withdrawal);
        transaction.SentAmount.Amount.Should().Be(0.015765m);
        transaction.SentAmount.CurrencyCode.Should().Be("BTC");
        transaction.FeeAmount.Amount.Should().Be(0m);
        transaction.FeeAmount.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void ExtractTransactions_WithMultipleTransactions_ShouldProcessAllInOrder()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,
664d24e5dc2198f4ddd2a6d7,2024-05-21 22:49:09 UTC,Withdrawal,CAD,500,USD,1,366.58,366.58,0,
663cec29e8ccfa4c2ddc099e,2024-05-09 15:30:49 UTC,Buy,DESO,16.760732234,USD,29.3937038741430442,359.96,365.33,5.362992009321613246,";
        var parser = CoinbaseCsvParser.Create(CreateStreamReader(csv)).Value;

        // Act
        var transactions = parser.ExtractTransactions().ToList();

        // Assert
        transactions.Should().HaveCount(3);
        var transaction1 = transactions[0] as FinancialTransaction;
        var transaction2 = transactions[1] as FinancialTransaction;
        var transaction3 = transactions[2] as FinancialTransaction;
        
        transaction1.Should().NotBeNull();
        transaction2.Should().NotBeNull();
        transaction3.Should().NotBeNull();
        
        transaction1!.Type.Should().Be(TransactionType.Deposit);
        transaction2!.Type.Should().Be(TransactionType.Withdrawal);
        transaction3!.Type.Should().Be(TransactionType.Trade);
    }

    [Fact]
    public void ExtractTransactions_WithIgnoredRefIds_ShouldSkipSpecifiedTransactions()
    {
        // Arrange
        var csv = $@"{ValidCsvHeader}
664fb52ee9e339613dd778da,2024-05-23 21:29:18 UTC,Deposit,CAD,1000,USD,1,728.31,728.31,0,
664d24e5dc2198f4ddd2a6d7,2024-05-21 22:49:09 UTC,Withdrawal,CAD,500,USD,1,366.58,366.58,0,";
        var ignoreRefIds = new[] { "664fb52ee9e339613dd778da" };
        var parser = CoinbaseCsvParser.Create(CreateStreamReader(csv), ignoreRefIds).Value;

        // Act
        var transactions = parser.ExtractTransactions().ToList();

        // Assert
        transactions.Should().HaveCount(1);
        var transaction = transactions[0] as FinancialTransaction;
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Withdrawal);
    }
}
