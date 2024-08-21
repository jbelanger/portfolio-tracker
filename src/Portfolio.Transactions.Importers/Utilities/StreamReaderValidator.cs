using CSharpFunctionalExtensions;

namespace Portfolio.Transactions.Importers.Utilities
{
    public static class StreamReaderValidator
    {
        public static Result ValidateStreamReader(StreamReader streamReader)
        {
            if (streamReader == null)
                return Result.Failure("StreamReader cannot be null.");

            if (streamReader.BaseStream == null)
                return Result.Failure("StreamReader's BaseStream is null.");

            if (!streamReader.BaseStream.CanRead)
                return Result.Failure("StreamReader's BaseStream cannot be read.");

            try
            {
                // Optionally, check if the stream has content
                if (streamReader.BaseStream.Length == 0)
                    return Result.Failure("StreamReader's BaseStream is empty.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Exception occurred while validating StreamReader: {ex.Message}");
            }

            return Result.Success();
        }
    }
}
