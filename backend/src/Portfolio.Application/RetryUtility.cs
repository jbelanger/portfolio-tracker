using Portfolio.Domain.Constants;

public static class RetryUtility
{
    /// <summary>
    /// Executes the specified operation with retry logic.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <param name="delay">The delay between retries.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the operation fails after the maximum number of retries.</exception>
    public static async Task<Result<T>> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3, TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromSeconds(2);

        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException httpEx)
            {
                // Log specific errors for HTTP request issues
                Log.Error(httpEx, "HTTP error occurred .");
            }
            catch (TimeoutException timeoutEx)
            {
                // Log timeout errors separately
                Log.Error(timeoutEx, "Timeout occurred.");
            }
            catch (Exception ex)
            {
                // General catch-all for unexpected errors
                Log.Error(ex, "Unexpected error in {MethodName}.", nameof(RetryAsync));
            }

            await Task.Delay(delay.Value);
        }

        return Result.Failure<T>($"HTTP request fails after {maxRetries} attemps.");
    }
}
