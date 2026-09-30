# End-to-end review demo

This project demonstrates the execution shape of the PR review workflow without calling an LLM.

It loads a real synthetic PR fixture, passes only the diff and allowed context to an `IChangeReviewer`, and prints the same structured findings/questions shape intended for a future model adapter.

The included `ReferenceChangeReviewer` is deliberately deterministic. It is **not** presented as an AI agent and is not intended to replace model evaluation. Its purpose is to prove the plumbing and make the agent contract observable before adding provider credentials and model variability.

## Run the cancellation example

```bash
dotnet run --project runner/AiEngineeringLab.Demo -- \
  --fixture agents/dotnet-pr-reviewer/evals/fixtures/dropped-cancellation
```

## Run the ownership example

```bash
dotnet run --project runner/AiEngineeringLab.Demo -- \
  --fixture agents/dotnet-pr-reviewer/evals/fixtures/ambiguous-stream-ownership
```

The next adapter can implement `IChangeReviewer` using an LLM while preserving the fixture and output contracts.
