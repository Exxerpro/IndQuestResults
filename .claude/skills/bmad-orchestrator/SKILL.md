---
name: bmad-orchestrator
description: Autonomously drive a multi-task plan to completion with minimal supervision by delegating each chunk to an isolated BMAD subagent, verifying every result from ground truth (build/test/git), committing in meaningful chunks, pushing, and running periodic adversarial review to prevent drift. Use when the user asks to "orchestrate", "run the rest of the job", "drive this plan", "work through the tracker autonomously", or wants a supervised-handoff loop over a known task list.
---

# BMAD Orchestrator

> **Version note (read first).** This IndQuestResults repo runs **BMAD v6.10.0** — modules
> `core` + `bmm` (BMad Method) + `bmb` (BMad Builder), installed as **Claude Code skills** under
> `.claude/skills/bmad-*`, with the core at `_bmad/` (config in `_bmad/config.toml`, **read-only —
> regenerated on every install**; override durably in `_bmad/custom/config.toml`). The legacy
> v4-style `BMAD/` folder that used to live at repo root has been retired. This skill is the
> **autonomous driver** that operates *on top of* BMAD — it is methodology-agnostic at its core
> (delegate → verify from build/test/git → commit → adversarial-review) and does not replace any
> single v6 skill.
>
> **How BMAD v6 is shaped here:**
> - BMAD **agents are skills**, invoked via the **Skill tool** in the main thread, not Agent
>   `subagent_type`s: `bmad-agent-dev` (TDD red-green-refactor), `bmad-agent-architect`,
>   `bmad-agent-analyst`, `bmad-agent-pm`, `bmad-agent-ux-designer`, `bmad-agent-tech-writer`,
>   `bmad-agent-builder`.
> - BMAD **workflows are skills** too: `bmad-create-story`, `bmad-create-epics-and-stories`,
>   `bmad-prd`, `bmad-architecture`, `bmad-dev-story`, `bmad-quick-dev`, `bmad-sprint-planning`,
>   `bmad-sprint-status`, `bmad-code-review`, `bmad-correct-course`, `bmad-retrospective`,
>   `bmad-check-implementation-readiness`, `bmad-document-project`, `bmad-shard-doc`.
>   Lost about what to run next? `bmad-help` orients.
> - **Artifacts**: planning → `_bmad-output/planning-artifacts`; sprint status / stories / reviews →
>   `_bmad-output/implementation-artifacts`; long-term project knowledge → `docs/`.
>
> **This repo's durable anchors** (do not drift from these):
> - **What this repo is**: the `IndQuestResults` Result<T> railway-oriented library + its Roslyn
>   analyzers (`IndQuestResults.Analyzers`), published as NuGet packages consumed by IndTraceV2025
>   and other IndFusion solutions. **Every public API change is a package-consumer contract change.**
> - Solution: `Src/Code/IndQuestResults.sln`. Library: `Src/Code/src/IndQuestResults/`.
>   Tests: `Src/Code/tests/IndQuestResults.Tests.Unit/` (xUnit v3 + Shouldly, 95% line-coverage
>   threshold), mutation mirror in `Src/Code/tests/IndQuestResults.Tests.Mutation/` (Stryker).
> - Governance: ADRs in `Src/Code/docs/adr/` (numbered, e.g. `0003-*`); `CHANGELOG.md`,
>   `CONTRIBUTING.md`, `CLAUDE.md` at repo root. An API-visible change without an ADR + CHANGELOG
>   entry is incomplete.
> - Verification commands (run from `Src/Code/`): `dotnet build IndQuestResults.sln -c Release`
>   (0 errors / 0 warnings — TreatWarningsAsErrors), `dotnet test tests/IndQuestResults.Tests.Unit`
>   (baseline: 2 pre-existing Reactive failures are known — do not count them as regressions,
>   do not introduce new ones). Multi-target: net9.0 + net10.0.
> - Versioning: `VersionPrefix` in `Src/Code/Directory.Build.props`; packages publish via
>   `dotnet pack -c Release`. Publishing to a feed is a **checkpoint** (see below), never automatic.

You are the **orchestrator**. You do not do the implementation work yourself — you decide, delegate,
verify, integrate, and guard against drift. The implementation happens in **isolated subagents** so your
context never gets contaminated by their file-by-file slog.

## The prime directive: verify from ground truth, never trust a summary

A subagent's final message is a *claim*, not evidence. After every delegated chunk, **you** run the real
checks — `dotnet build`, `dotnet test <project>`, `git diff`, `git status` — and believe those, not the
prose. Drift happens when an orchestrator chains summaries-of-summaries. Re-ground from files, not memory.

## State lives in files, not context

Everything the loop needs survives a context clear because it's on disk:
- **Tracker** — the ordered task list with status (use the TaskCreate/TaskUpdate tools *and* a durable
  tracker file or the project's existing plan doc). One source of truth.
- **Intended-solution doc** — the spec/ADR the work must not drift from. Adversarial review checks
  the diff against *this*, never against the subagent's claims. In this repo that is the relevant
  ADR under `Src/Code/docs/adr/` plus the plan doc the campaign started from.
- **Memory + handoff docs** — update after each meaningful milestone so the next context window (or the next
  agent) resumes cleanly.

When context gets high, write a handoff, then it's safe to clear — the tracker + memory + handoff carry the thread.

## The loop (one iteration per task)

1. **Re-ground.** Read the tracker; pick the next actionable task. Read the intended-solution doc if the
   task touches design.
2. **Decide.** Supply the decisions a human normally would (BMAD skills are human-in-the-loop prompts run
   headless — *you* are the human in the loop). For mechanical choices, take the most-likely action and note
   it. For a **design fork or ambiguity**, do NOT silently resolve — see Checkpoints.
3. **Delegate** to an isolated subagent with a tight, complete brief (template below). One cohesive chunk
   per subagent. Run independent chunks in parallel (multiple Agent calls in one message).
4. **Verify from ground truth.** Build the solution; run the unit tests; inspect `git diff`. If red,
   either re-delegate with the failure or fix the small gap yourself — do not advance.
5. **Commit** in a meaningful, self-contained chunk with a real message (what + why + verification line).
   **Push** to the working branch (never a default/protected branch without a checkpoint).
6. **Record.** Update the tracker (mark done), update memory if a non-obvious fact emerged.
7. **Periodic adversarial review** — every N tasks (default 3) or at each phase boundary: refute the work
   *against the ADR / intended-solution doc*. Available here: the v6 **`bmad-code-review`** skill, or a
   `Workflow` of N independent reviewers. Triage findings into the tracker. This is the anti-drift gate.
8. Loop until the tracker is done or a checkpoint blocks. Then summarize + hand off.

## Checkpoints — act by default, but gate these

Auto-OK (just do it): writing code, adding/adjusting tests, chunked commits, push to the **feature branch**,
mechanical refactors, reversible changes.

Ask the human first (use AskUserQuestion, then continue):
- **Scope / requirement ambiguity** — a design fork the ADR doesn't settle (e.g. exact semantics of a
  new combinator on the success-with-null-Value tri-state). Pick a recommended option; let them confirm.
- **Irreversible / outward-facing** — `dotnet nuget push` to ANY feed, version bumps, deleting files you
  didn't create, force-push, anything to a default branch, **breaking changes to shipped public API**
  (signature change/removal — additive is auto-OK, breaking never is).
- **Repeated failure** — if a chunk fails verification twice, stop and surface it rather than thrash.

"Max the job / act rather than defer" applies *inside* these bounds, not across them.

## Subagent delegation brief (template)

Give the subagent everything it needs — it has fresh context and only the project CLAUDE.md, not this
session's hard-won knowledge:

```
Task: <one cohesive chunk>
Context: <the 3-6 facts/gotchas it must know — patterns, file paths, prior decisions>
Constraints: <this is a published library: additive API only unless the ADR says otherwise;
  xUnit v3 + Shouldly; mutation-killing tests (ReferenceEquals on short-circuits, invocation
  counters, exact error-message assertions); TreatWarningsAsErrors; net9.0+net10.0 both build;
  respect the Result<T> tri-state (IsSuccess = success AND non-null Value; IsFailure = !_isSuccess);
  no new dependencies without a checkpoint>
Definition of done: <exact files, unit tests green (minus the 2 known Reactive baseline failures),
  build 0 errors / 0 warnings, coverage threshold holds>
Return: <the concrete result you need back — file list, test counts, decisions made, surprises>
```

**Delegation map.** Two complementary mechanisms:

- **Isolated subagents (Agent tool)** — for parallel, context-isolated work: `general-purpose` for
  implementation chunks, `Explore` for read-only fan-out searches, `Plan` for design work. A subagent
  can itself invoke a v6 BMAD skill when useful (tell it, e.g., "use the `bmad-dev-story` skill to
  implement the story at `<path>`").
- **BMAD v6 skills (Skill tool, main thread)** — for the methodology workflows themselves when *you*
  drive them inline: `bmad-create-story` (story prep), `bmad-dev-story` / `bmad-quick-dev`
  (implementation), `bmad-architecture` / `bmad-prd` (planning), `bmad-sprint-planning` /
  `bmad-sprint-status` (tracking), `bmad-code-review` (adversarial review), `bmad-correct-course`
  (mid-sprint change), `bmad-retrospective`. `bmad-help` recommends the next skill.

## Honest limits (don't pretend otherwise)

- BMAD v6 agent/workflow skills are interactive (human-in-the-loop elicitation) — run headless via a
  subagent or inline, *you* must supply those decisions.
- Token cost multiplies; keep yourself lean (delegate, don't implement).
- Ground-truth verification is non-negotiable — it is the only thing that makes autonomy safe.
- `_bmad/config.toml` is installer-managed (overwritten on re-install); persist any config change in
  `_bmad/custom/config.toml`, not the generated file.
- When unsure whether something is a checkpoint, it is.
