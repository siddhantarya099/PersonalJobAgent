using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonalJobAgent.Application.DTOs;

public sealed class ExtractedCandidateProfile
{
    public decimal ExperienceYears { get; set; }

    public string CurrentRole { get; set; } = string.Empty;

    public string CurrentLocation { get; set; } = string.Empty;

    [JsonConverter(typeof(FlexibleSkillCollectionConverter))]
    public IReadOnlyCollection<string> Skills { get; set; } = [];

    public IReadOnlyCollection<ExtractedSkillEvidence> SkillEvidence { get; set; } = [];
}

public sealed class ExtractedSkillEvidence
{
    public string Skill { get; set; } = string.Empty;

    public string Evidence { get; set; } = string.Empty;
}

public sealed class FlexibleSkillCollectionConverter : JsonConverter<IReadOnlyCollection<string>>
{
    public override IReadOnlyCollection<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            var objectValue = document.RootElement;
            if (objectValue.TryGetProperty("skill", out var skill) && skill.ValueKind == JsonValueKind.String)
            {
                return [skill.GetString()!];
            }

            if (objectValue.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            {
                return [name.GetString()!];
            }

            return objectValue.EnumerateObject()
                .Select(property => property.Name)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
        }

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Skills must be a JSON array.");
        }

        var skills = new List<string>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    skills.Add(value);
                }
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                var value = item.TryGetProperty("skill", out var skill)
                    ? skill.GetString()
                    : item.TryGetProperty("name", out var name) ? name.GetString() : null;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    skills.Add(value);
                }
            }
        }

        return skills;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyCollection<string> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}