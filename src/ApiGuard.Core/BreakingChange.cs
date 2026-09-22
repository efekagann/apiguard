namespace ApiGuard.Core;

public enum ChangeSeverity
{
    Breaking,
    Warning,
}

public record BreakingChange(string Path, string? Method, ChangeSeverity Severity, string Description)
{
    public override string ToString()
    {
        var location = Method is null ? Path : $"{Method} {Path}";
        var tag = Severity == ChangeSeverity.Breaking ? "BREAKING" : "WARNING";
        return $"[{tag}] {location}: {Description}";
    }
}
