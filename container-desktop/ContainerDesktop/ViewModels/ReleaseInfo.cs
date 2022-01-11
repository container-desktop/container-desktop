using System.Text.Json.Serialization;

namespace ContainerDesktop.ViewModels;

public class ReleaseInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; }
}
