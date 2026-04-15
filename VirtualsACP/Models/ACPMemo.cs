using System.Text.Json;
using System.Text.Json.Serialization;

namespace VirtualsAcp.Models;

public class ACPMemo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("nextPhase")]
    public AcpJobPhase NextPhase { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("signedReason")]
    public string? SignedReason { get; set; }

    [JsonPropertyName("expiry")]
    public DateTime? Expiry { get; set; }

    [JsonIgnore]
    public GenericPayload? StructuredContent { get; set; }

    public ACPMemo()
    {
        // Try to parse structured content from the content string
        if (!string.IsNullOrEmpty(Content))
        {
            try
            {
                StructuredContent = JsonSerializer.Deserialize<GenericPayload>(Content);
            }
            catch
            {
                // If parsing fails, leave StructuredContent as null
            }
        }
    }

    public override string ToString()
    {
        var properties = new Dictionary<string, object>
        {
            ["id"] = Id,
            ["type"] = Type,
            ["content"] = Content,
            ["nextPhase"] = NextPhase,
            ["status"] = Status,
            ["signedReason"] = SignedReason ?? "",
            ["expiry"] = Expiry?.ToString("o") ?? ""
        };

        return $"AcpMemo({JsonSerializer.Serialize(properties)})";
    }

    public PayloadType? PayloadType => StructuredContent?.Type;

    public T? GetDataAs<T>() where T : class
    {
        if (StructuredContent?.Data == null) return null;

        try
        {
            if (StructuredContent.Data is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element);
            }
            else if (StructuredContent.Data is List<object> list)
            {
                var json = JsonSerializer.Serialize(list);
                return JsonSerializer.Deserialize<T>(json);
            }
            else
            {
                var json = JsonSerializer.Serialize(StructuredContent.Data);
                return JsonSerializer.Deserialize<T>(json);
            }
        }
        catch
        {
            return null;
        }
    }
}
