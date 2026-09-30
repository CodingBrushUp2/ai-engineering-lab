# Skill contract

Given a diff plus relevant surrounding code, review only evidence visible in the supplied context.

Prioritize cancellation propagation, async correctness, resource ownership, exception behavior, public API and serialization compatibility, unbounded buffering, disposal and tests around changed boundary behavior.

Return findings ordered by severity. Every finding must identify the concrete code or behavior that triggered it and explain the production consequence.

Do not invent missing architecture or requirements. If a concern depends on unavailable context, return a question rather than asserting a defect.

If no material issue is supported by the supplied code, return no material finding. Do not manufacture style comments.

## Non-goals

This skill does not score code quality, enforce style preferences, redesign architecture, review product requirements or claim a change is safe because no finding was produced.
