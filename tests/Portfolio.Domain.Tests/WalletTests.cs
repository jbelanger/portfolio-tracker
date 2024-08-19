using FluentAssertions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Tests
{
    [TestFixture]
    public class WalletTests
    {
        [Test]
        public void Create_ShouldReturnFailure_WhenNameIsEmpty()
        {
            // Act
            var result = Wallet.Create(string.Empty);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Name cannot be empty.");
        }

        [Test]
        public void Create_ShouldSucceed_WhenNameIsValid()
        {
            // Arrange
            var transactions = new List<CryptoCurrencyRawTransaction>();

            // Act
            var result = Wallet.Create("Test Wallet");

            // Assert
            result.IsSuccess.Should().BeTrue();
            var wallet = result.Value;
            wallet.Name.Should().Be("Test Wallet");
            wallet.Transactions.Should().BeEmpty();
        }

        [Test]
        public void AddTransaction_ShouldAddTransaction_WhenTransactionIsValid()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateWithdraw(
                DateTime.Now,
                new Money(1.0m, "BTC"),
                new Money(0.1m, "BTC"),
                "TestAccount",
                new List<string> { "tx1" },
                "Test Note"
            ).Value;

            var walletResult = Wallet.Create("Test Wallet");
            var wallet = walletResult.Value;

            // Act
            var addResult = wallet.AddTransaction(transaction);

            // Assert
            addResult.IsSuccess.Should().BeTrue();
            wallet.Transactions.Should().ContainSingle(t => t == transaction);
        }

        [Test]
        public void AddTransaction_ShouldFail_WhenTransactionAlreadyExists()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateWithdraw(
                DateTime.Now,
                new Money(1.0m, "BTC"),
                new Money(0.1m, "BTC"),
                "TestAccount",
                new List<string> { "tx1" },
                "Test Note"
            ).Value;

            var walletResult = Wallet.Create("Test Wallet");
            var wallet = walletResult.Value;

            // Act
            var addResult1 = wallet.AddTransaction(transaction);
            var addResult2 = wallet.AddTransaction(transaction);

            // Assert
            addResult1.IsSuccess.Should().BeTrue();
            addResult2.IsFailure.Should().BeTrue();
            addResult2.Error.Should().Be("Transaction already exists in this holding.");
            wallet.Transactions.Should().HaveCount(1);
        }

        [Test]
        public void RemoveTransaction_ShouldRemoveTransaction_WhenTransactionExists()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateWithdraw(
                DateTime.Now,
                new Money(1.0m, "BTC"),
                new Money(0.1m, "BTC"),
                "TestAccount",
                new List<string> { "tx1" },
                "Test Note"
            ).Value;

            var walletResult = Wallet.Create("Test Wallet");
            var wallet = walletResult.Value;

            // Act
            var addResult1 = wallet.AddTransaction(transaction);
            var removeResult = wallet.RemoveTransaction(transaction);

            // Assert
            addResult1.IsSuccess.Should().BeTrue();
            removeResult.IsSuccess.Should().BeTrue();
            wallet.Transactions.Should().BeEmpty();
        }

        [Test]
        public void RemoveTransaction_ShouldFail_WhenTransactionDoesNotExist()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateWithdraw(
                DateTime.Now,
                new Money(1.0m, "BTC"),
                new Money(0.1m, "BTC"),
                "TestAccount",
                new List<string> { "tx1" },
                "Test Note"
            ).Value;

            var walletResult = Wallet.Create("Test Wallet");
            var wallet = walletResult.Value;

            // Act
            var removeResult = wallet.RemoveTransaction(transaction);

            // Assert
            removeResult.IsFailure.Should().BeTrue();
            removeResult.Error.Should().Be("Transaction not found in this holding.");
            wallet.Transactions.Should().BeEmpty();
        }
    }
}
