using FluentAssertions;
using Portfolio.Transactions.Importers.Utilities;

namespace Portfolio.Transactions.Importers.Tests.Utilities
{
    [TestFixture]
    public class StreamReaderValidatorTests
    {
        [Test]
        public void ValidateStreamReader_ShouldReturnFailure_WhenStreamReaderIsNull()
        {
            // Arrange
            StreamReader? nullStreamReader = null;

            // Act
            var result = StreamReaderValidator.ValidateStreamReader(nullStreamReader);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("StreamReader cannot be null.");
        }

        [Test]
        public void ValidateStreamReader_ShouldReturnFailure_WhenBaseStreamIsEmpty()
        {
            // Arrange
            var emptyStream = new MemoryStream();
            var streamReader = new StreamReader(emptyStream);

            // Act
            var result = StreamReaderValidator.ValidateStreamReader(streamReader);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("StreamReader's BaseStream is empty.");
        }

        [Test]
        public void ValidateStreamReader_ShouldReturnFailure_WhenSampleReadFails()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            streamWriter.WriteLine("sample data");
            streamWriter.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(memoryStream);

            // Act
            // Close the stream to make it unreadable after this point
            streamReader.BaseStream.Close();

            var result = StreamReaderValidator.ValidateStreamReader(streamReader);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().StartWith("StreamReader's BaseStream cannot be read.");
        }

        [Test]
        public void ValidateStreamReader_ShouldReturnSuccess_WhenStreamReaderIsValid()
        {
            // Arrange
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            string expectedFirstLine = "expected data";
            streamWriter.WriteLine(expectedFirstLine);
            streamWriter.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(memoryStream);

            // Act
            var result = StreamReaderValidator.ValidateStreamReader(streamReader);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }
}
