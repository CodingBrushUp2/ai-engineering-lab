# End-to-end review demo

This project runs one synthetic PR fixture through an `IChangeReviewer` and prints the structured review defined in [agent.md](../../agents/dotnet-pr-reviewer/agent.md).

```text
fixture directory
  → FixtureLoader          reads diff.patch + context.md only
  → ReviewInput            the complete model-facing input
  → IChangeReviewer        reference (deterministic) or openai (model-backed, opt-in)
  → ReviewResult           validated: summary, findings[], questions[]
  → --record (optional)    {caseId, observed, reviewer, review} for the Evals runner
```

Evaluator metadata (`cases.json`: `expect`, `focus`, `description`) is never read here. See [the fixture README](../../agents/dotnet-pr-reviewer/evals/fixtures/README.md) for why.

## Reviewers

- `reference` (default): `ReferenceChangeReviewer` is deliberately deterministic string matching. It is **not** an AI agent. It exists to exercise the plumbing in CI without credentials or model variability.
- `openai`: `OpenAiCompatibleChangeReviewer` sends one request to an OpenAI-compatible Chat Completions endpoint using strict structured outputs (`response_format: json_schema`). It is the only provider-specific code and lives in `OpenAi/`. Its output is rejected, not repaired, if the model refuses, is truncated, returns malformed JSON or violates the contract.

## Run with the reference reviewer

```bash
dotnet run --project runner/AiEngineeringLab.Demo -- \
  --fixture agents/dotnet-pr-reviewer/evals/fixtures/dropped-cancellation
```

## Run one fixture through a real model (opt-in, paid)

The model reviewer reads its configuration from environment variables only. Never put a key in a file inside the repository.

| Variable | Required | Meaning |
| --- | --- | --- |
| `AI_LAB_OPENAI_API_KEY` | yes | API key for the endpoint. A generic `OPENAI_API_KEY` is deliberately ignored. |
| `AI_LAB_OPENAI_MODEL` | yes | Model name. It must support strict JSON-schema structured outputs. |
| `AI_LAB_OPENAI_BASE_URL` | no | Defaults to `https://api.openai.com/v1`. Plain `http` is accepted only for localhost. |

```bash
export AI_LAB_OPENAI_API_KEY=...        # set in your shell, not in a committed file
export AI_LAB_OPENAI_MODEL=<model-name>

dotnet run --project runner/AiEngineeringLab.Demo -- \
  --fixture agents/dotnet-pr-reviewer/evals/fixtures/dropped-cancellation \
  --reviewer openai \
  --record artifacts/dropped-cancellation.observation.json

dotnet run --project runner/AiEngineeringLab.Evals -- \
  --cases agents/dotnet-pr-reviewer/evals/cases.json \
  --result artifacts/dropped-cancellation.observation.json
```

`artifacts/` is git-ignored so local model recordings are not committed by accident.

Each run makes exactly one request with no retries. The input is a small diff, the context and the instructions, and the output is a short JSON document. Cost is therefore small but not zero, and it depends on the model you choose. Output is not deterministic: repeated runs can differ.

## Recorded observation

`--record` writes:

```json
{
  "caseId": "dropped-cancellation",
  "observed": "finding",
  "reviewer": "openai-compatible:<model>",
  "review": { "summary": "...", "findings": [ ... ], "questions": [ ... ] }
}
```

`caseId` is the fixture directory name. `observed` is derived after inference: any finding → `finding`, otherwise any question → `question`, otherwise `no-finding`. This mapping is coarse on purpose and matches the vocabulary in `cases.json`. The Evals runner scores `caseId` and `observed` and ignores the other fields.

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Review produced (and recorded, if requested). |
| 1 | Review failed or was cancelled. Nothing is recorded. |
| 2 | Invalid arguments, missing fixture files or missing configuration. |
