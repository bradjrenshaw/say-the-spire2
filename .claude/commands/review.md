Run all three code review agents in parallel on the current diff.

## Instructions

Launch three agents in parallel, each performing a different review of the current changes:

1. **Bug Hunter** — Run `/review-bugs` to check for correctness issues, null safety, logic errors, side effects, and regressions.

2. **Simplicity Reviewer** — Run `/review-simplify` to check for code duplication, unnecessary complexity, dead code, and over-engineering.

3. **Convention Enforcer** — Run `/review-conventions` to check compliance with CLAUDE.md invariants, AGENTS.md checklists, and codebase patterns.

After all three complete, present a unified summary:

### Summary
- **Bugs**: [count] issues found ([critical count] critical)
- **Simplification**: [count] opportunities found
- **Conventions**: [count] violations found

Then list all findings grouped by severity:
1. Critical (must fix before commit)
2. Warnings (should fix)
3. Notes (consider fixing)
