# Evaluation runner

The first runner is intentionally local and provider-neutral.

It validates recorded outcomes without calling a model or requiring credentials. This keeps the evaluation contract separate from provider integration while the fixtures are still evolving.

## Run

```bash
dotnet run --project runner/AiEngineeringLab.Evals -- --result runner/examples/dropped-cancellation.result.json
```

Exit code 0 means the observed outcome matches the expected outcome. Exit code 1 means the case failed. Invalid input returns 2.

V1 is not an LLM harness yet. The next step is to turn the evaluation manifests into complete fixtures with input context and assertions, then add a provider adapter behind the same result format.
