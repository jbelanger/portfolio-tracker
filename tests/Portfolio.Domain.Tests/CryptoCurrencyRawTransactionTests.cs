using FluentAssertions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Tests
{
    [TestFixture]
    public class CryptoCurrencyRawTransactionTests
    {
        private static Money CreateMoney(decimal amount, string currencyCode) =>
            new Money(amount, currencyCode);

        [Test]
        public void CreateDeposit_ShouldCreateValidDepositTransaction()
        {
            // Arrange
            var date = new DateTime(2024, 8, 19, 10, 0, 0);
            var receivedAmount = CreateMoney(100m, "USD");
            var feeAmount = CreateMoney(1m, "USD");
            var account = "Account1";
            var transactionIds = new List<string> { "tx123" };

            // Act
            var result = CryptoCurrencyRawTransaction.CreateDeposit(date, receivedAmount, feeAmount, account, transactionIds);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var transaction = result.Value;
            transaction.DateTime.Should().Be(date);
            transaction.Type.Should().Be(TransactionType.Deposit);
            transaction.ReceivedAmount.Should().Be(receivedAmount.ToAbsoluteAmountMoney());
            transaction.FeeAmount.Should().Be(feeAmount.ToAbsoluteAmountMoney());
            transaction.Account.Should().Be(account);
            transaction.TransactionIds.Should().BeEquivalentTo(transactionIds);
        }

        [Test]
        public void CreateDeposit_ShouldFailWhenReceivedAmountIsNull()
        {
            // Arrange
            var date = new DateTime(2024, 8, 19, 10, 0, 0);
            Money receivedAmount = null;
            var feeAmount = CreateMoney(1m, "USD");
            var account = "Account1";
            var transactionIds = new List<string> { "tx123" };

            // Act
            var result = CryptoCurrencyRawTransaction.CreateDeposit(date, receivedAmount, feeAmount, account, transactionIds);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("Received amount cannot be null for a deposit.");
        }

        [Test]
        public void CreateWithdraw_ShouldCreateValidWithdrawTransaction()
        {
            // Arrange
            var date = new DateTime(2024, 8, 19, 10, 0, 0);
            var amount = CreateMoney(50m, "USD");
            var feeAmount = CreateMoney(0.5m, "USD");
            var account = "Account1";
            var transactionIds = new List<string> { "tx456" };

            // Act
            var result = CryptoCurrencyRawTransaction.CreateWithdraw(date, amount, feeAmount, account, transactionIds);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var transaction = result.Value;
            transaction.DateTime.Should().Be(date);
            transaction.Type.Should().Be(TransactionType.Withdrawal);
            transaction.SentAmount.Should().Be(amount.ToAbsoluteAmountMoney());
            transaction.FeeAmount.Should().Be(feeAmount.ToAbsoluteAmountMoney());
            transaction.Account.Should().Be(account);
            transaction.TransactionIds.Should().BeEquivalentTo(transactionIds);
        }

        [Test]
        public void CreateTrade_ShouldCreateValidTradeTransaction()
        {
            // Arrange
            var date = new DateTime(2024, 8, 19, 10, 0, 0);
            var receivedAmount = CreateMoney(100m, "USD");
            var sentAmount = CreateMoney(50m, "USD");
            var feeAmount = CreateMoney(2m, "USD");
            var account = "Account1";
            var transactionIds = new List<string> { "tx789" };

            // Act
            var result = CryptoCurrencyRawTransaction.CreateTrade(date, receivedAmount, sentAmount, feeAmount, account, transactionIds);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var transaction = result.Value;
            transaction.DateTime.Should().Be(date);
            transaction.Type.Should().Be(TransactionType.Trade);
            transaction.ReceivedAmount.Should().Be(receivedAmount.ToAbsoluteAmountMoney());
            transaction.SentAmount.Should().Be(sentAmount.ToAbsoluteAmountMoney());
            transaction.FeeAmount.Should().Be(feeAmount.ToAbsoluteAmountMoney());
            transaction.Account.Should().Be(account);
            transaction.TransactionIds.Should().BeEquivalentTo(transactionIds);
        }

        [Test]
        public void Equals_ShouldReturnTrueForEqualTransactions()
        {
            // Arrange
            var date = new DateTime(2024, 8, 19, 10, 0, 0);
            var receivedAmount = CreateMoney(100m, "USD");
            var sentAmount = CreateMoney(50m, "USD");
            var feeAmount = CreateMoney(2m, "USD");
            var account = "Account1";
            var transactionIds = new List<string> { "tx789" };

            var transaction1 = CryptoCurrencyRawTransaction.CreateTrade(date, receivedAmount, sentAmount, feeAmount, account, transactionIds).Value;
            var transaction2 = CryptoCurrencyRawTransaction.CreateTrade(date, receivedAmount, sentAmount, feeAmount, account, transactionIds).Value;

            // Act
            var areEqual = transaction1.Equals(transaction2);

            // Assert
            areEqual.Should().BeTrue();
        }

        [Test]
        public void Equals_ShouldReturnFalseForDifferentTransactions()
        {
            // Arrange
            var date1 = new DateTime(2024, 8, 19, 10, 0, 0);
            var date2 = new DateTime(2024, 8, 19, 10, 0, 1);
            var receivedAmount = CreateMoney(100m, "USD");
            var sentAmount = CreateMoney(50m, "USD");
            var feeAmount = CreateMoney(2m, "USD");
            var account = "Account1";
            var transactionIds = new List<string> { "tx789" };

            var transaction1 = CryptoCurrencyRawTransaction.CreateTrade(date1, receivedAmount, sentAmount, feeAmount, account, transactionIds).Value;
            var transaction2 = CryptoCurrencyRawTransaction.CreateTrade(date2, receivedAmount, sentAmount, feeAmount, account, transactionIds).Value;

            // Act
            var areEqual = transaction1.Equals(transaction2);

            // Assert
            areEqual.Should().BeFalse();
        }
    }
}
