# AI Engineering Lab

A public collection of small agents, reusable skills and evaluation experiments built to understand how AI-assisted engineering behaves under explicit constraints.

The goal is not to collect prompts. Each artifact should have a narrow purpose, documented boundaries and evaluation cases that include failure and no-finding scenarios.

## Built here

### Agents
- [.NET PR Review Agent](agents/dotnet-pr-reviewer/) — read-only pull-request review workflow with selective context gathering and structured findings.

### Skills
- [.NET Change Reviewer](skills/dotnet-change-reviewer/) — evidence-based review capability for common production risks in C#/.NET changes.

## Repository structure

```text
agents/       Agent workflows and their evaluations
skills/       Reusable capabilities with explicit contracts
experiments/  Focused comparisons and recorded results
```

Artifacts created in this repository are kept separate from third-party work. If external agents or skills are studied or adapted later, their source, license and attribution will be recorded explicitly.

## Principles

- narrow scope before autonomy;
- evidence before confidence;
- evaluation cases before broader permissions;
- questions instead of invented context;
- zero findings is a valid result;
- no production credentials or secrets in examples.

## Status

Experimental. The first artifacts are specifications and evaluation fixtures. A small runner will follow so behavior can be measured across model and configuration changes.

## License

MIT
