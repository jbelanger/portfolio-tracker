using System.Text;
using FluentAssertions;
using Portfolio.Api.Services;
using Portfolio.App.Tests.Utilities;
using Portfolio.Infrastructure;
using Portfolio.Transactions.Importers.Csv.Kraken;

namespace Portfolio.App.Tests
{
    public class CryptoTransactionServiceTests
    {
        private PortfolioDbContext _dbContext;

        [SetUp]
        public void SetUp()
        {
            _dbContext = DbContextUtilities.GetMemoryDbContext();
            var portfolio = FakeDataHelper.AddPortfolio(_dbContext);
            var wallet = FakeDataHelper.AddWallet(_dbContext, portfolio);
            _dbContext.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        [Test]
        public async Task ImportTransactionsFromCsvAsync_WhenValidImportParameters_ReturnsSuccess()
        {
            CryptoTransactionService svc = new CryptoTransactionService(_dbContext);

            List<string> lines = new List<string>
            {
                KrakenCsvParser.EXPECTED_FILE_HEADER,
                "TX1,TX1,2023-02-09 01:41:55,deposit,,currency,CAD,spot / main,1000.0000,14.7800,985.2200"
            };

            // Use StringBuilder to construct the string with CRLF line endings
            StringBuilder sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            // Convert the StringBuilder content to a byte array
            byte[] byteArray = Encoding.UTF8.GetBytes(sb.ToString());

            // Create a MemoryStream from the byte array
            using (MemoryStream memoryStream = new MemoryStream(byteArray))
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                var result = await svc.ImportTransactionsFromCsvAsync(1, 1, CsvFileImportType.Kraken, reader);
                result.IsSuccess.Should().BeTrue();
            }
        }
    }
}