using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace Portfolio.App.Extensions;

public static class RetryExtensions
{
    public static async Task<HttpResponseMessage> WithRetry<T>(
        this Func<Task<HttpResponseMessage>> operation,
        int retryCount,
        TimeSpan delay)
    {
        int currentRetry = 0;
        HttpResponseMessage? response = null;

        while (currentRetry < retryCount)
        {
            response = await operation().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            currentRetry++;
            Log.Warning("Retrying operation. Attempt {RetryCount}/{MaxRetries}", currentRetry, retryCount);

            // Wait before retrying
            await Task.Delay(delay).ConfigureAwait(false);
        }

        // Return a failure result after exhausting all retries
        return response ?? new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
    }
}
