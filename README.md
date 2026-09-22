# ApiGuard

A GitHub Action that detects **breaking changes** between two versions of an OpenAPI/Swagger spec and fails your CI before they reach production.

It catches things like:

- Removed endpoints or operations
- New required parameters
- Parameters that became required
- Parameter or field type changes
- Required fields added to a request body
- Fields removed from a response

## Usage

Your workflow needs to produce two files: the spec as it exists on the base branch, and the spec as it exists on the PR branch. Then hand both to ApiGuard:

```yaml
name: API Contract Check

on:
  pull_request:

permissions:
  contents: read
  pull-requests: write

jobs:
  check-breaking-changes:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Get base spec
        run: git show origin/${{ github.base_ref }}:openapi.yaml > old-spec.yaml

      - uses: efekagann/apiguard@v1
        with:
          old-spec-path: old-spec.yaml
          new-spec-path: openapi.yaml
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

If a breaking change is found, the action exits non-zero and fails the workflow run, with the list of changes printed in the job log. When run on a `pull_request` event with `GITHUB_TOKEN` set, it also posts the change list as a PR comment (the `pull-requests: write` permission above is required for that).

## Local usage

```bash
dotnet run --project src/ApiGuard.Cli -- old-spec.yaml new-spec.yaml
```

## Development

```bash
dotnet test
docker build -t apiguard .
docker run apiguard examples/old-spec.yaml examples/new-spec.yaml
```
