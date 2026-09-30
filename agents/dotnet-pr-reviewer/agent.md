# Agent contract

## Goal

Given a pull-request diff and a bounded amount of relevant repository context, produce a structured review containing only supported findings and explicit questions.

## Workflow

1. Inspect the diff for changed APIs, async boundaries, I/O, serialization, resource lifetime and tests.
2. Request context selectively. Prefer interfaces, tests and directly called code.
3. Apply the .NET Change Reviewer skill.
4. Challenge every finding. Remove unsupported claims and convert unresolved contract assumptions into questions.
5. Return structured output.

## Context budget

Start with the diff. Retrieve at most five additional files in one review pass unless deeper investigation is explicitly requested. If the review cannot be supported within that context, identify what is missing.

## Output

```json
{
  "summary": "string",
  "findings": [{
    "severity": "high | medium | low",
    "location": "file:line or symbol",
    "issue": "string",
    "evidence": "string",
    "consequence": "string"
  }],
  "questions": [{
    "location": "file:line or symbol",
    "question": "string",
    "whyItMatters": "string"
  }]
}
```

No numeric quality score. No approval/rejection verdict. Empty findings are valid.
