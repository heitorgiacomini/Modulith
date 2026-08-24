# Consolidate GitHub agent instructions

## Summary

- First save this finalized plan as `.github/plan/github-instructions-consolidation-plan.md`.
- Consolidate `src/.github` into the repository-root `.github`.
- Delete `src/.github` after resolving all overlaps.
- Make Plan agent persist finalized plans without automatically starting implementation.

## Changes

- Keep root-only agents, plans, checklists, and skills unchanged.
- For identical duplicates (`Plan.agent.md` and `concise-responses/SKILL.md`), retain the root copies.
- Merge relevant architecture rules, native-skill guidance, and validation guidance from `src/.github/copilot-instructions.md` into the root instructions.
- Resolve conflicts in favor of current repository reality and root-relative paths; keep root `.github/copilot-instructions.md` canonical.
- Delete the complete tracked `src/.github` directory after consolidation.

- Update root `Plan.agent.md` to:
  - retain memory-backed drafts;
  - allow `edit` solely for finalized plans under `.github/plan/`;
  - follow the root `AGENTS.md` filename, revision, and overwrite rules;
  - never edit application code;
  - save the decision-complete plan before offering a handoff;
  - change the implementation handoff to `send: false`, so clicking it only switches/prefills the implementation agent;
  - require a subsequent explicit user prompt before any code execution begins.

## Verification

- Confirm `src/.github` no longer exists.
- Confirm no root-only `.github` files were removed or overwritten.
- Confirm the consolidated Copilot instructions contain no duplicate or conflicting working-directory guidance.
- Validate `Plan.agent.md` frontmatter and confirm VS Code recognizes its tools and handoffs.
- Confirm finalized plans can be written only beneath `.github/plan/`.
- Confirm clicking the implementation handoff sends no automatic execution request.
- Run `git diff --check` and review the tracked rename/deletion set.

## Assumptions

- Root instructions win when the two Copilot instruction files conflict.
- Draft plans remain in VS Code memory; the finalized plan file under `.github/plan/` is the durable repository copy.
- A handoff click does not authorize implementation. Execution starts only after the user explicitly submits a separate implementation prompt.
