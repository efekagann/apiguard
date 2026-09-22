using ApiGuard.Core;
using Microsoft.OpenApi.Readers;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: apiguard <old-spec-path> <new-spec-path>");
    return 2;
}

var oldPath = args[0];
var newPath = args[1];

if (!File.Exists(oldPath))
{
    Console.Error.WriteLine($"Old spec not found: {oldPath}");
    return 2;
}

if (!File.Exists(newPath))
{
    Console.Error.WriteLine($"New spec not found: {newPath}");
    return 2;
}

var reader = new OpenApiStreamReader();

using var oldStream = File.OpenRead(oldPath);
var oldDoc = reader.Read(oldStream, out var oldDiag);

using var newStream = File.OpenRead(newPath);
var newDoc = reader.Read(newStream, out var newDiag);

if (oldDiag.Errors.Count > 0)
{
    Console.Error.WriteLine($"Failed to parse old spec: {string.Join("; ", oldDiag.Errors)}");
    return 2;
}

if (newDiag.Errors.Count > 0)
{
    Console.Error.WriteLine($"Failed to parse new spec: {string.Join("; ", newDiag.Errors)}");
    return 2;
}

var changes = OpenApiDiffer.Compare(oldDoc, newDoc);

if (changes.Count == 0)
{
    Console.WriteLine("No breaking changes detected.");
    return 0;
}

foreach (var change in changes)
{
    Console.WriteLine(change);
}

var hasBreaking = changes.Any(c => c.Severity == ChangeSeverity.Breaking);
Console.WriteLine();
Console.WriteLine($"{changes.Count(c => c.Severity == ChangeSeverity.Breaking)} breaking, {changes.Count(c => c.Severity == ChangeSeverity.Warning)} warning.");

return hasBreaking ? 1 : 0;
