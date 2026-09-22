using Microsoft.OpenApi.Models;

namespace ApiGuard.Core;

public static class OpenApiDiffer
{
    public static IReadOnlyList<BreakingChange> Compare(OpenApiDocument oldDoc, OpenApiDocument newDoc)
    {
        var changes = new List<BreakingChange>();

        foreach (var (path, oldPathItem) in oldDoc.Paths)
        {
            if (!newDoc.Paths.TryGetValue(path, out var newPathItem))
            {
                changes.Add(new BreakingChange(path, null, ChangeSeverity.Breaking, "Endpoint removed"));
                continue;
            }

            foreach (var (operationType, oldOp) in oldPathItem.Operations)
            {
                var method = operationType.ToString().ToUpperInvariant();

                if (!newPathItem.Operations.TryGetValue(operationType, out var newOp))
                {
                    changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking, "Operation removed"));
                    continue;
                }

                CompareParameters(path, method, oldOp, newOp, changes);
                CompareRequestBody(path, method, oldOp, newOp, changes);
                CompareResponses(path, method, oldOp, newOp, changes);
            }
        }

        return changes;
    }

    private static void CompareParameters(string path, string method, OpenApiOperation oldOp, OpenApiOperation newOp, List<BreakingChange> changes)
    {
        var oldParams = (oldOp.Parameters ?? new List<OpenApiParameter>())
            .ToDictionary(p => (p.Name, p.In));

        foreach (var newParam in newOp.Parameters ?? new List<OpenApiParameter>())
        {
            var key = (newParam.Name, newParam.In);

            if (!oldParams.TryGetValue(key, out var oldParam))
            {
                if (newParam.Required)
                {
                    changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking,
                        $"New required parameter '{newParam.Name}' ({newParam.In})"));
                }
                continue;
            }

            if (newParam.Required && !oldParam.Required)
            {
                changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking,
                    $"Parameter '{newParam.Name}' became required"));
            }

            var oldType = oldParam.Schema?.Type;
            var newType = newParam.Schema?.Type;
            if (oldType is not null && newType is not null && oldType != newType)
            {
                changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking,
                    $"Parameter '{newParam.Name}' type changed from '{oldType}' to '{newType}'"));
            }
        }
    }

    private static void CompareRequestBody(string path, string method, OpenApiOperation oldOp, OpenApiOperation newOp, List<BreakingChange> changes)
    {
        var oldSchema = GetJsonSchema(oldOp.RequestBody?.Content);
        var newSchema = GetJsonSchema(newOp.RequestBody?.Content);

        if (newOp.RequestBody?.Required == true && oldOp.RequestBody?.Required != true)
        {
            changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking, "Request body became required"));
        }

        CompareRequiredFields(path, method, "Request body", oldSchema, newSchema, changes);
    }

    private static void CompareRequiredFields(string path, string method, string context, OpenApiSchema? oldSchema, OpenApiSchema? newSchema, List<BreakingChange> changes)
    {
        if (newSchema?.Required is null || newSchema.Required.Count == 0)
        {
            return;
        }

        var oldRequired = oldSchema?.Required ?? new HashSet<string>();

        foreach (var field in newSchema.Required)
        {
            if (!oldRequired.Contains(field))
            {
                changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking,
                    $"{context}: new required field '{field}'"));
            }
        }

        var oldProps = oldSchema?.Properties ?? new Dictionary<string, OpenApiSchema>();
        foreach (var (propName, newPropSchema) in newSchema.Properties ?? new Dictionary<string, OpenApiSchema>())
        {
            if (oldProps.TryGetValue(propName, out var oldPropSchema)
                && oldPropSchema.Type is not null
                && newPropSchema.Type is not null
                && oldPropSchema.Type != newPropSchema.Type)
            {
                changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking,
                    $"{context}: field '{propName}' type changed from '{oldPropSchema.Type}' to '{newPropSchema.Type}'"));
            }
        }
    }

    private static void CompareResponses(string path, string method, OpenApiOperation oldOp, OpenApiOperation newOp, List<BreakingChange> changes)
    {
        foreach (var (statusCode, _) in oldOp.Responses)
        {
            if (!newOp.Responses.ContainsKey(statusCode))
            {
                changes.Add(new BreakingChange(path, method, ChangeSeverity.Warning,
                    $"Response '{statusCode}' removed"));
            }
        }

        foreach (var (statusCode, oldResponse) in oldOp.Responses)
        {
            if (!newOp.Responses.TryGetValue(statusCode, out var newResponse))
            {
                continue;
            }

            var oldSchema = GetJsonSchema(oldResponse.Content);
            var newSchema = GetJsonSchema(newResponse.Content);

            if (oldSchema?.Properties is null || newSchema?.Properties is null)
            {
                continue;
            }

            foreach (var (propName, oldPropSchema) in oldSchema.Properties)
            {
                if (!newSchema.Properties.ContainsKey(propName))
                {
                    changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking,
                        $"Response '{statusCode}': field '{propName}' removed"));
                }
                else
                {
                    var newPropSchema = newSchema.Properties[propName];
                    if (oldPropSchema.Type is not null && newPropSchema.Type is not null && oldPropSchema.Type != newPropSchema.Type)
                    {
                        changes.Add(new BreakingChange(path, method, ChangeSeverity.Breaking,
                            $"Response '{statusCode}': field '{propName}' type changed from '{oldPropSchema.Type}' to '{newPropSchema.Type}'"));
                    }
                }
            }
        }
    }

    private static OpenApiSchema? GetJsonSchema(IDictionary<string, OpenApiMediaType>? content)
    {
        if (content is null)
        {
            return null;
        }

        return content.TryGetValue("application/json", out var mediaType) ? mediaType.Schema : null;
    }
}
