using FluentAssertions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Transactions.Exporters;

namespace Portfolio.Tests.Transactions.Exporters
{
    [TestFixture]
    public class KoinlyCsvExporterTests
    {
        [Test]
        public void Constructor_Should_ThrowArgumentNullException_When_TransactionsAreNull()
        {
            // Act
            Action act = () => new KoinlyCsvExporter(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().WithMessage("*transactions*");
        }

        [Test]
        public void GetCsvLines_Should_IncludeHeader_When_WithHeaderIsTrue()
        {
            // Arrange
            var transactions = new List<CryptoCurrencyTransaction>();
            var exporter = new KoinlyCsvExporter(transactions);

            // Act
            var result = exporter.GetCsvLines(withHeader: true).ToList();

            // Assert
            result.Should().NotBeEmpty();
            result.First().Should().Be("Date,Sent Amount,Sent Currency,Received Amount,Received Currency,Fee Amount,Fee Currency,Net Worth Amount,Net Worth Currency,Label,Description,TxHash");
        }

        [Test]
        public void GetCsvLines_Should_NotIncludeHeader_When_WithHeaderIsFalse()
        {
            // Arrange
            var transactions = new List<CryptoCurrencyTransaction>();
            var exporter = new KoinlyCsvExporter(transactions);

            // Act
            var result = exporter.GetCsvLines(withHeader: false).ToList();

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public void GetCsvLines_Should_ReturnCorrectCsvLine_For_DepositTransaction()
        {
            // Arrange
            var transaction = CryptoCurrencyDepositTransaction.Create(        
                date: DateTime.SpecifyKind(DateTime.Parse("2023-08-19 12:00:00"), DateTimeKind.Utc),
                receivedAmount: new Money(1.23m, "BTC"),
                feeAmount: new Money(0.01m, "BTC"),
                note: "Deposit",
                account: "TestAccount",
                transactionIds: new List<string> { "txid1" }
            ).Value;
            var transactions = new List<CryptoCurrencyTransaction> { transaction };
            var exporter = new KoinlyCsvExporter(transactions);

            // Act
            var result = exporter.GetCsvLines(withHeader: false).ToList();

            // Assert
            result.Should().HaveCount(1);
            result.First().Should().Be("2023-08-19 12:00:00 UTC,,,1.23,BTC,0.01,BTC,,,Deposit,TestAccount,txid1");
        }

        [Test]
        public void GetCsvLines_Should_ReturnCorrectCsvLine_For_WithdrawTransaction()
        {
            // Arrange
            var transaction = CryptoCurrencyWithdrawTransaction.Create(
                date: DateTime.SpecifyKind(DateTime.Parse("2023-08-19 12:00:00"), DateTimeKind.Utc),
                amount: new Money(1.23m, "BTC"),
                feeAmount: new Money(0.01m, "BTC"),
                note: "Withdraw",
                account: "TestAccount",
                transactionIds: new List<string> { "txid2" }
            ).Value;
            var transactions = new List<CryptoCurrencyTransaction> { transaction };
            var exporter = new KoinlyCsvExporter(transactions);

            // Act
            var result = exporter.GetCsvLines(withHeader: false).ToList();

            // Assert
            result.Should().HaveCount(1);
            result.First().Should().Be("2023-08-19 12:00:00 UTC,1.23,BTC,,,0.01,BTC,,,Withdraw,TestAccount,txid2");
        }

        [Test]
        public void GetCsvLines_Should_ReturnCorrectCsvLine_For_TradeTransaction()
        {
            // Arrange
            var transaction = CryptoCurrencyTradeTransaction.Create(
                date: DateTime.SpecifyKind(DateTime.Parse("2023-08-19 12:00:00"), DateTimeKind.Utc),
                receivedAmount: new Money(1.23m, "BTC"),
                sentAmount: new Money(25000m, "USD"),
                feeAmount: new Money(0.01m, "BTC"),
                note: "Trade",
                account: "TestAccount",
                transactionIds: new List<string> { "txid3" }
            ).Value;
            var transactions = new List<CryptoCurrencyTransaction> { transaction };
            var exporter = new KoinlyCsvExporter(transactions);

            // Act
            var result = exporter.GetCsvLines(withHeader: false).ToList();

            // Assert
            result.Should().HaveCount(1);
            result.First().Should().Be("2023-08-19 12:00:00 UTC,25000,USD,1.24,BTC,0.01,BTC,,,Trade,TestAccount,txid3");
        }

        [Test]
        public void WriteToFile_Should_WriteCsvLines_ToFile()
        {
            // Arrange
            var mockFilePath = Path.GetTempFileName();
            var transaction = CryptoCurrencyDepositTransaction.Create(
                date: DateTime.SpecifyKind(DateTime.Parse("2023-08-19 12:00:00"), DateTimeKind.Utc),
                receivedAmount: new Money(1.23m, "BTC"),
                feeAmount: new Money(0.01m, "BTC"),
                note: "Deposit",
                account: "TestAccount",
                transactionIds: new List<string> { "txid1" }
            ).Value;
            var transactions = new List<CryptoCurrencyTransaction> { transaction };
            var exporter = new KoinlyCsvExporter(transactions);

            // Act
            exporter.WriteToFile(mockFilePath);

            // Assert
            var writtenLines = File.ReadAllLines(mockFilePath);
            writtenLines.Should().ContainSingle(line => line.Contains("1.23,BTC"));
        }
    }
}
