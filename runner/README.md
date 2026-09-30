# Evaluation runner

The first runner is intentionally local and provider-neutral.

Expected outcomes live in the canonical evaluation manifest. Recorded model or human observations live in a separate result document, so a result cannot redefine what counts as passing.

## Run

```bash
dotnet run --project runner/AiEngineeringLab.Evals -- \
  --cases agents/dotnet-pr-reviewer/evals/cases.json \
  --result runner/examples/dropped-cancellation.result.json
```

Exit code 0 means the observed outcome matches the canonical expectation. Exit code 1 means the case failed. Invalid input returns 2.

V1 is not an LLM harness yet. Next, the manifests need complete input fixtures and richer assertions before a provider adapter is added.
