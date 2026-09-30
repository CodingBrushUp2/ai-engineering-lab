# Review fixtures

Fixtures are small, synthetic pull-request inputs used to evaluate the review workflow.

Each fixture may contain:

- `diff.patch`: the change under review.
- `context.md`: only the repository or contract context the reviewer is allowed to know.

Expected outcomes remain in `../cases.json`. Keeping expectations outside the input fixture prevents the reviewer from seeing the answer it is being evaluated against.

Fixtures should be minimal enough to isolate one behavior while remaining plausible .NET changes. Do not use production code, credentials or internal company details.

## Evaluation boundary

The model-facing input is the fixture content only. A provider adapter must not include `cases.json`, `expect`, `focus` or the case description in the prompt. Those fields are evaluator metadata and may reveal the intended outcome.

The runner may read evaluator metadata after inference to score the recorded observation.
