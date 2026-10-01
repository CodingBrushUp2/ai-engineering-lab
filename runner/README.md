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

The runner scores one recorded observation at a time. Observations can come from the deterministic reference reviewer or from an opt-in model-backed reviewer; see [AiEngineeringLab.Demo](AiEngineeringLab.Demo/README.md) for how to produce one with `--record`. CI only uses the reference reviewer and never calls a model.

Scoring is still coarse (`finding` / `question` / `no-finding`). Richer assertions, for example checking the finding's location or evidence, are future work.
