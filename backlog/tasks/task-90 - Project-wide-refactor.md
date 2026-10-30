---
id: TASK-90
title: Project-wide refactor
status: Done
assignee: []
created_date: '2026-10-30 11:24'
updated_date: '2026-10-30 12:56'
labels:
  - refactor
dependencies: []
ordinal: 139000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Analyse the whole codebase for SOLID, clarity and readability problems, split the findings into independent subtasks that each own their files, run them in parallel, then a post-refactor cleanup. Findings are listed below in priority order.
<!-- SECTION:DESCRIPTION:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
## Findings (priority order)

Analysis covered Domain, Services/Storage/Program.cs, Components/wwwroot/Styles and the test project. No file is over 300 lines; the problems are in how responsibilities are split, not length.

1. **High: day editing model is built the wrong way (TASK-90.1).** DayWork needs the saved rows passed into every call and exposes mutable state that PeriodSheet, WorklogSync and DayRow reach into; the "can this day be saved" rule is written twice and planning mutates drafts. Rebuild around one SheetDay and one Domain save plan.
2. **High: Timesheet.razor runs the whole period workflow (TASK-90.7).** ~200-line @code block; DayPanel holds time-off flow and calls back into the page. Move it into a tested ShownPeriod.
3. **High: the Jira connection leaks into components (TASK-90.10).** WorklogSync takes a connection from the page, DayEditor injects JiraClient and threads a search delegate through three components; the signed-in guard is copied four times.
4. **Medium: TimeEntry mixes four roles and its sync state is set by hand in four classes; "same work" is compared four slightly different ways (TASK-90.8).**
5. **Medium: Jira JSON mapping lives in the REST clients, and FormatStarted uses the server's time zone (TASK-90.6).** Some Jira rules are tested 2–3 times or via stale FailWhen workarounds.
6. **Medium: "unknown hours" and "not in Jira" rules are copied across components and PeriodLoader (TASK-90.11).**
7. **Medium: the owner filter is re-implemented per service; test-only API lives in production (CurrentUser.SignedIn, GetDayAsync, AllAsync); static state in LegacyDataMigration (TASK-90.12).**
8. **Medium: the test harness hand-wires services that Program.cs also wires; TimeHarness.cs holds five concepts; FakeJira setup is pasted across tests (TASK-90.13).**
9. **Medium: fifteen test files have their own builders and several test files are named after things that don't exist (TASK-90.14).**
10. **Medium: ExportPage mixes four jobs with index-based column styling; download/copy JS is duplicated; a dead style (TASK-90.3).**
11. **Medium: ExpiringCache never evicts in a singleton; a second hand-rolled expiry (TASK-90.5).**
12. **Medium: weekend/holiday look and time-off badge duplicated; colour picker inside Format.cs (TASK-90.9).**
13. **Medium: request-log display rules duplicated in two components; logging types inside JiraHttp; log models mixed into Domain's top level (TASK-90.2).**
14. **Low: Settings and Connect parse input in the page, untested; one-file Setup folder (TASK-90.4).**
15. **Low: Services/ is a flat grab-bag of 21 files (TASK-90.15).**
16. **Cleanup (TASK-90.16):** docs, app.css, dead code, full test run and browser check.

**Not included because they change behaviour (for Carl to decide):** JiraIssueLookup's singleton caches serve one user's issue lookups to another user on the same host; the time-off write itself doesn't run as a Jira wait, so other Jira buttons stay enabled during it.

## Execution plan

Every subtask owns its files; subtasks that share files are ordered by dependencies (checked by script). Only the cleanup edits AGENTS.md, README, doc-1 and wwwroot/app.css.
- Wave 1 (parallel): 90.1, 90.2, 90.3, 90.4, 90.5
- Then: 90.6 and 90.7 (after 90.1) → 90.8 → 90.9 → 90.10 → 90.11 → 90.12 → 90.13 → 90.14 → 90.15 → 90.16
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
All 16 subtasks are Done and on main; behaviour is unchanged (browser checks against the mock Jira matched before and after) and the suite grew from 513 to 566 tests.

What changed:
- Day editing (90.1): one SheetDay per day reads and edits saved rows and unsaved changes together; DaySave is the single save plan Save all sends; days off go through PeriodSheet.
- Jira (90.6): Jira's JSON format lives in JiraWorklogJson and JiraIssueJson; the REST clients only make calls; the start time uses TimeProvider's zone.
- Timesheet (90.7): the period workflow moved out of Timesheet.razor into ShownPeriod.
- TimeEntry (90.8): sync fields change only through named transitions; one rule for same work and comments (no comment = empty); Description became Comment (JSON name kept); start/end entries are converted by the legacy migration.
- Looks (90.9): DayLook owns weekend, holiday and time-off looks; IssueColors moved out of Format.
- Connection (90.10): CurrentUser.Jira is the only source of the connection; issue search moved out of components into IssueStatuses.
- Summary (90.11): PeriodSummary owns unknown hours and work not in Jira; components no longer combine those rules.
- Data (90.12): OwnedCollection is the one owner filter; test-only production API removed.
- Tests (90.13, 90.14): the harness is built from the app's DI (AddJiraTime); shared builders (TestEntries, TestSheets, TestUsers); test files named after what they test.
- Smaller (90.2–90.5): request log models grouped; Export page split with one browser file script (files.js); Settings and Connect rules in Domain; ExpiringCache drops expired values.
- Layout (90.15): Services grouped into Jira, JiraLog, SignIn and Worklogs.
- Cleanup (90.16): shims and dead code removed, docs and app.css updated.

Old bugs found and kept as they were (not part of a behaviour-preserving refactor): the Save all notice is wiped when the first unsaved day isn't the one on screen; a period reached through Leave without saving has no slide animation; typing only https:// in Connect becomes https://https:.
<!-- SECTION:FINAL_SUMMARY:END -->
