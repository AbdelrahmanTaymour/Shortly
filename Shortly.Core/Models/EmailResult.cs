namespace Shortly.Core.Models;

public record EmailResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public Exception? Exception { get; set; }

    public static EmailResult Success() => new EmailResult { IsSuccess = true, Message = "Email sent successfully" };
    public static EmailResult Failure(string message, Exception? ex = null) => new EmailResult { IsSuccess = false, Message = message, Exception = ex };
}