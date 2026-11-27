# Agent instructions (Claude, Codex, Gemini, others)

## Sources of truth

1. `docs/adr/`: architecture decisions. A decision changes only through a new ADR that supersedes
   the old one, in its own pull request.
2. `docs/spec.md`: current design of the product (tiers, modules, architecture, coexistence rules).
   It follows the ADRs; when they disagree, the ADR wins and the spec is fixed.
3. `DESIGN.md` (once added): visual language and tokens of the settings window.
4. GitHub issues: needs and backlog. Issues describe needs, not decisions.

`docs/research/` holds research inputs. They inform decisions; they are not decisions. Cite the
ADR, not the research report. A new report goes to `docs/research/YYYY-MM-DD-<topic>.md`, its
prompt to `YYYY-MM-DD-<topic>-prompt.md`, and the report starts with a provenance block (model,
date, revision examined) and the coordinator's verification notes. The report body stays as the
model wrote it, except for links.

## Rules

- English everywhere in the repository and on GitHub: code, comments, docs, commit messages,
  issues, pull requests. Exception: a research report in `docs/research/` may be in the language
  its prompt asked for; its provenance block and notes are in English.
- Dates written in the repository or on GitHub are in **UTC**, ISO form (`2026-11-27`,
  `2026-11-27T19:46Z`). Take them from the clock, never from memory. No relative time words
  ("yesterday", "recently") in the repository.
- No new dependency without a demonstrated need, named in the pull request.
- No fixed-interval polling in the core: react to Windows notifications (spec, principle 6).
- Read settings from Windows at the moment they matter; never hardcode a timeout or a device.
- The core runs without admin rights. Anything that needs elevation lives in the optional
  elevated helper.
- Keep machine-specific data out of the repository: no user names, local paths, serial numbers,
  monitor instance IDs or logs from a real machine. Model hardware IDs (EDID like `AUS2700`) in a
  quirks table are fine.

## Pull requests

- Never push to `main`. Every change lands through a focused pull request into `main`: one per
  decision (ADR), reviewed slice of implementation, or documentation change. Don't mix an ADR with
  the code that depends on it: the ADR comes first, in its own pull request.
- Every commit is pushed to its branch. Branch names: `<type>/<short-topic>` with `type` one of
  `feat`, `fix`, `docs`, `adr`, `chore`, `proto`.
- Pull requests follow `.github/pull_request_template.md` and name the issues they advance
  (`Refs #N`) or close (`Closes #N`, only when every acceptance criterion is met), or say
  `No issue:` with a one-line reason.
- A pull request may be stacked on another when it extends it: say so in its summary and never
  merge it before its base. Merge with a merge commit (no squash, no rebase-merge) so the stack
  stays valid, then retarget the next pull request to `main`.
- Merging into `main` needs the owner's explicit approval for that pull request. With it, an agent
  may merge; without it, nobody does. Approval for one pull request does not extend to another, and
  auto-merge is never enabled without the owner asking. Never force-push, rewrite or reset `main`.
- Review reports (code review, audit, verification of a pull request) are posted as comments on
  the pull request or issue concerned, naming the commit SHA they examined. They are not committed.
- Run the checks that exist for the touched area before every push; a red push is fixed first.

## Issues and labels

GitHub issues are the only tracking system.

- Status, exactly one per open issue:
  - `status:backlog`: accepted, not planned;
  - `status:ready`: acceptance and evidence are clear, work can start;
  - `status:in-progress`: someone works on it;
  - `status:review`: its pull request awaits review;
  - `status:blocked`: say why in a comment;
  - `status:verified`: acceptance shown in a comment, waiting for the owner to close it.
- Area, at least one per issue: `area:core`, `area:monitors`, `area:cpu`, `area:gpu`, `area:ui`,
  `area:platform` (build, CI, dependencies, packaging), `area:docs`, `area:governance`.
- `priority:p0` to `priority:p3`: set by the owner only; an agent may propose one in a comment.
- Workstreams are parent issues; a slice that one pull request can deliver is a GitHub sub-issue
  of its parent, one level deep. A sub-issue's body names its parent (`Parent #N`). A parent is
  done when its own acceptance is shown, not because its sub-issues are closed.
- Starting work sets `status:in-progress`; opening its pull request sets `status:review`. When a
  slice needs more than one pull request, the issue goes back to `status:in-progress` after each
  partial merge and only the last pull request says `Closes`. A merged pull request does not close
  an issue unless its acceptance is fully met.

## Delegation to other agents

- One writer at a time for overlapping files.
- A delegate's report is evidence, not a decision: the coordinator verifies its claims before
  anything enters an ADR or the spec.
