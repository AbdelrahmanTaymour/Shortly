namespace Shortly.Core.Entities;

/// <summary>
/// Define the ShortUrl class which acts as an entity model class to store Url details in the data store 
/// </summary>
public class ShortUrl
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; }
    public string? ShortCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int AccessCount { get; set; }
}