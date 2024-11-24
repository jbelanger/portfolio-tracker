#undef PERF_TEST_ENABLE

using FluentAssertions;

namespace Portfolio.Tests
{
    [TestFixture]
    public class DateTimeExtensionsTests
    {
        [Test]
        public void TruncateToSecond_ShouldRemoveMilliseconds()
        {
            // Arrange
            var dateTime = new DateTime(2024, 8, 19, 10, 30, 45, 123, DateTimeKind.Utc);
            var expected = new DateTime(2024, 8, 19, 10, 30, 45, DateTimeKind.Utc);

            // Act
            var result = dateTime.TruncateToSecond();

            // Assert
            result.Should().Be(expected);
        }

        [Test]
        public void TruncateToSecond_ShouldPreserveDateAndTime()
        {
            // Arrange
            var dateTime = new DateTime(2024, 8, 19, 10, 30, 45, 999, DateTimeKind.Local);
            var expected = new DateTime(2024, 8, 19, 10, 30, 45, DateTimeKind.Local);

            // Act
            var result = dateTime.TruncateToSecond();

            // Assert
            result.Should().Be(expected);
        }

        [Test]
        public void TruncateToSecond_ShouldHandleMinValue()
        {
            // Arrange
            var dateTime = DateTime.MinValue;
            var expected = DateTime.MinValue;

            // Act
            var result = dateTime.TruncateToSecond();

            // Assert
            result.Should().Be(expected);
        }

        [Test]
        public void TruncateToSecond_ShouldHandleMaxValue()
        {
            // Arrange
            var dateTime = DateTime.MaxValue;
            var expected = new DateTime(DateTime.MaxValue.Year, DateTime.MaxValue.Month, DateTime.MaxValue.Day,
                                        DateTime.MaxValue.Hour, DateTime.MaxValue.Minute, DateTime.MaxValue.Second,
                                        DateTime.MaxValue.Kind);

            // Act
            var result = dateTime.TruncateToSecond();

            // Assert
            result.Should().Be(expected);
        }

#if PERF_TEST_ENABLE
        private static Money CreateMoney(decimal amount, string currencyCode) =>
            new Money(amount, currencyCode);

        [Test]
        public void TruncateToSecond_PerformanceTest()
        {
            // Skip the test if running in a CI environment
            var isCiEnvironment = Environment.GetEnvironmentVariable("CI_ENVIRONMENT") == "true";
            if (isCiEnvironment)
            {
                Assert.Ignore("Skipping performance test in CI environment.");
                return;
            }

            // Arrange
            const int numberOfTransactions = 100000000; // Ten million
            var date = DateTime.Now;
            var receivedAmount = CreateMoney(100m, "USD");
            var feeAmount = CreateMoney(1m, "USD");
            var account = "Account1";
            var transactionIds = new List<string> { "tx123" };
            var transactions = new List<CryptoCurrencyRawTransaction>(numberOfTransactions);

            var result = CryptoCurrencyRawTransaction.CreateDeposit(date, receivedAmount, feeAmount, account, transactionIds);
            var transaction = result.Value;

            // Measure the performance of TruncateToSecond
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < numberOfTransactions; i++)
            {
                // This is where we are invoking TruncateToSecond to simulate real usage
                var truncatedDate = transaction.DateTime.TruncateToSecond();
                transactions.Add(transaction);
            }

            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            // Output the time taken to the test result for review
            TestContext.WriteLine($"Time taken to add {numberOfTransactions} transactions: {elapsedMilliseconds} ms");

            // Optional: Assert that the operation completes in a reasonable amount of time
            elapsedMilliseconds.Should().BeLessThan(5000, "Performance test failed, operation took too long."); // Example threshold
        }

#endif
    }
}
