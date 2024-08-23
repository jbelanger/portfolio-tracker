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
            result.Error.Should().Contain("Received amount must be greater than zero.");
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
        public void SetTransactionAmounts_Should_ReturnFailure_When_ReceivedAmount_IsInvalid_ForDeposit()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateDeposit(DateTime.UtcNow, new Money(100, "USD"), null, "Account1", null).Value;

            // Act
            var result = transaction.SetTransactionAmounts(new Money(0, "USD"), null, null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Received amount must be greater than zero.");
        }

        [Test]
        public void SetTransactionAmounts_Should_ReturnFailure_When_SentAmount_IsInvalid_ForWithdrawal()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateWithdraw(DateTime.UtcNow, new Money(100, "USD"), null, "Account1", null).Value;

            // Act
            var result = transaction.SetTransactionAmounts(null, new Money(0, "USD"), null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Sent amount must be greater than zero.");
        }

        [Test]
        public void SetTransactionAmounts_Should_ReturnFailure_When_ReceivedAmount_IsSet_ForWithdrawal()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateWithdraw(DateTime.UtcNow, new Money(100, "USD"), null, "Account1", null).Value;

            // Act
            var result = transaction.SetTransactionAmounts(new Money(100, "USD"), null, null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Received amount can not be set on a 'withdrawal' transaction.");
        }

        [Test]
        public void SetTransactionAmounts_Should_ReturnFailure_When_SentAmount_IsSet_ForDeposit()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateDeposit(DateTime.UtcNow, new Money(100, "USD"), null, "Account1", null).Value;

            // Act
            var result = transaction.SetTransactionAmounts(new Money(100, "USD"), new Money(100, "USD"), null);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Sent amount can not be set on a 'deposit' transaction.");
        }

        [Test]
        public void SetTransactionAmounts_Should_ReturnSuccess_When_ValidAmounts_AreSet_ForTrade()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateTrade(DateTime.UtcNow, new Money(100, "USD"), new Money(50, "USD"), null, "Account1", null).Value;

            // Act
            var result = transaction.SetTransactionAmounts(new Money(100, "USD"), new Money(50, "USD"), new Money(5, "USD"));

            // Assert
            result.IsSuccess.Should().BeTrue();
            transaction.ReceivedAmount.Should().Be(new Money(100, "USD"));
            transaction.SentAmount.Should().Be(new Money(50, "USD"));
            transaction.FeeAmount.Should().Be(new Money(5, "USD"));
        }

        [Test]
        public void SetTransactionAmounts_Should_ReturnSuccess_When_ValidAmounts_AreSet_ForDeposit()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateDeposit(DateTime.UtcNow, new Money(100, "USD"), null, "Account1", null).Value;

            // Act
            var result = transaction.SetTransactionAmounts(new Money(100, "USD"), null, new Money(1, "USD"));

            // Assert
            result.IsSuccess.Should().BeTrue();
            transaction.ReceivedAmount.Should().Be(new Money(100, "USD"));
            transaction.SentAmount.Should().Be(Money.Empty);
            transaction.FeeAmount.Should().Be(new Money(1, "USD"));
        }

        [Test]
        public void SetTransactionAmounts_Should_ReturnSuccess_When_ValidAmounts_AreSet_ForWithdrawal()
        {
            // Arrange
            var transaction = CryptoCurrencyRawTransaction.CreateWithdraw(DateTime.UtcNow, new Money(100, "USD"), null, "Account1", null).Value;

            // Act
            var result = transaction.SetTransactionAmounts(null, new Money(100, "USD"), new Money(1, "USD"));

            // Assert
            result.IsSuccess.Should().BeTrue();
            transaction.ReceivedAmount.Should().Be(Money.Empty);
            transaction.SentAmount.Should().Be(new Money(100, "USD"));
            transaction.FeeAmount.Should().Be(new Money(1, "USD"));
        }
    }
}
