namespace PersonalJobAgent.Application.JobMatching.Services;

public interface ISkillCanonicalizer
{
    /// <summary>
    /// Gets the canonical lookup key for a skill name or alias.
    /// Used for robust internal equality and set operations.
    /// </summary>
    string GetCanonicalKey(string skill);

    /// <summary>
    /// Gets the standard canonical display name for a skill name or alias.
    /// If not found in known aliases, returns the trimmed original skill name.
    /// </summary>
    string GetCanonicalName(string skill);

    /// <summary>
    /// Checks if two skill representations refer to the same canonical technology.
    /// </summary>
    bool AreMatching(string skillA, string skillB);

    /// <summary>
    /// Converts a collection of skills into a unique set of canonical keys.
    /// </summary>
    IReadOnlySet<string> GetCanonicalKeys(IEnumerable<string> skills);
}

