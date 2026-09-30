# .NET PR Review Agent

A read-only agent workflow for reviewing C#/.NET pull-request changes.

It starts from a diff, gathers a bounded amount of relevant context, applies the [.NET Change Reviewer](../../skills/dotnet-change-reviewer/) skill, challenges unsupported findings and returns structured findings and questions.

## Boundaries

V1 may read a diff and relevant repository files. It does not modify code, post review comments, approve pull requests, merge branches or call production systems.

See [agent.md](agent.md) for the workflow contract and [evals/cases.json](evals/cases.json) for the initial evaluation set.
