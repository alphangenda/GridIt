using System.Text.Json.Serialization;

namespace Infrastructure.Mailing;

public class SendGridError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("help")]
    public string? Help { get; set; }
}