using Portfolio.Domain.Common;

public class HttpRequestLogEntry: BaseEntity
{
    public DateTime RequestDate { get; set; }
    public string RequestUri { get; set; } = string.Empty;
}