using ApiGuard.Core;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace ApiGuard.Tests;

public class OpenApiDifferTests
{
    private static OpenApiDocument Parse(string yaml)
    {
        var reader = new OpenApiStringReader();
        var doc = reader.Read(yaml, out var diagnostic);
        Assert.Empty(diagnostic.Errors);
        return doc;
    }

    private const string BaseSpec = """
        openapi: 3.0.0
        info:
          title: Test API
          version: "1.0"
        paths:
          /users/{id}:
            get:
              parameters:
                - name: id
                  in: path
                  required: true
                  schema:
                    type: string
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        properties:
                          id:
                            type: string
                          name:
                            type: string
        """;

    [Fact]
    public void NoChanges_ReturnsEmpty()
    {
        var oldDoc = Parse(BaseSpec);
        var newDoc = Parse(BaseSpec);

        var changes = OpenApiDiffer.Compare(oldDoc, newDoc);

        Assert.Empty(changes);
    }

    [Fact]
    public void RemovedEndpoint_IsBreaking()
    {
        var oldDoc = Parse(BaseSpec);
        var newDoc = Parse("""
            openapi: 3.0.0
            info:
              title: Test API
              version: "1.0"
            paths: {}
            """);

        var changes = OpenApiDiffer.Compare(oldDoc, newDoc);

        var change = Assert.Single(changes);
        Assert.Equal(ChangeSeverity.Breaking, change.Severity);
        Assert.Equal("/users/{id}", change.Path);
    }

    [Fact]
    public void NewRequiredQueryParameter_IsBreaking()
    {
        var oldDoc = Parse(BaseSpec);
        var newDoc = Parse("""
            openapi: 3.0.0
            info:
              title: Test API
              version: "1.0"
            paths:
              /users/{id}:
                get:
                  parameters:
                    - name: id
                      in: path
                      required: true
                      schema:
                        type: string
                    - name: includeDeleted
                      in: query
                      required: true
                      schema:
                        type: boolean
                  responses:
                    "200":
                      description: OK
                      content:
                        application/json:
                          schema:
                            type: object
                            properties:
                              id:
                                type: string
                              name:
                                type: string
            """);

        var changes = OpenApiDiffer.Compare(oldDoc, newDoc);

        var change = Assert.Single(changes);
        Assert.Equal(ChangeSeverity.Breaking, change.Severity);
        Assert.Contains("includeDeleted", change.Description);
    }

    [Fact]
    public void RemovedResponseField_IsBreaking()
    {
        var oldDoc = Parse(BaseSpec);
        var newDoc = Parse("""
            openapi: 3.0.0
            info:
              title: Test API
              version: "1.0"
            paths:
              /users/{id}:
                get:
                  parameters:
                    - name: id
                      in: path
                      required: true
                      schema:
                        type: string
                  responses:
                    "200":
                      description: OK
                      content:
                        application/json:
                          schema:
                            type: object
                            properties:
                              id:
                                type: string
            """);

        var changes = OpenApiDiffer.Compare(oldDoc, newDoc);

        var change = Assert.Single(changes);
        Assert.Equal(ChangeSeverity.Breaking, change.Severity);
        Assert.Contains("name", change.Description);
    }

    [Fact]
    public void NewOptionalParameter_IsNotBreaking()
    {
        var oldDoc = Parse(BaseSpec);
        var newDoc = Parse("""
            openapi: 3.0.0
            info:
              title: Test API
              version: "1.0"
            paths:
              /users/{id}:
                get:
                  parameters:
                    - name: id
                      in: path
                      required: true
                      schema:
                        type: string
                    - name: verbose
                      in: query
                      required: false
                      schema:
                        type: boolean
                  responses:
                    "200":
                      description: OK
                      content:
                        application/json:
                          schema:
                            type: object
                            properties:
                              id:
                                type: string
                              name:
                                type: string
            """);

        var changes = OpenApiDiffer.Compare(oldDoc, newDoc);

        Assert.Empty(changes);
    }
}
