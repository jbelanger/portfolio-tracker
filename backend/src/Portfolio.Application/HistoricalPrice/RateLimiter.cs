public class RateLimiter
{
    private readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
    private DateTime _lastRequestTime = DateTime.MinValue;

    public int RequestsPerMinute { get; private set; }

    public RateLimiter(int requestsPerMinute)
    {
        if (requestsPerMinute <= 0)
            throw new ArgumentException("RequestsPerMinute must be greater than 0.", nameof(requestsPerMinute));

        RequestsPerMinute = requestsPerMinute;
    }

    public async Task EnsureRateLimitAsync()
    {
        await _rateLimitSemaphore.WaitAsync();

        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var delay = TimeSpan.FromMinutes(1.0 / RequestsPerMinute);

            if (timeSinceLastRequest < delay)
            {
                await Task.Delay(delay - timeSinceLastRequest);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public void UpdateRequestsPerMinute(int newRequestsPerMinute)
    {
        if (newRequestsPerMinute <= 0)
            throw new ArgumentException("RequestsPerMinute must be greater than 0.", nameof(newRequestsPerMinute));

        RequestsPerMinute = newRequestsPerMinute;
    }
}
