---
id: TASK-7
title: Plan tools that help the user do time registration
status: Done
assignee: []
created_date: '2026-10-19 09:15'
updated_date: '2026-10-19 11:45'
labels: []
dependencies: []
type: spike
ordinal: 7000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Planning phase, no implementation. Work out with Carl which tools would make time registration faster and more accurate, and turn the agreed ideas into backlog tasks with acceptance criteria.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Candidate tools discussed and prioritised with Carl
- [x] #2 Each agreed tool has its own backlog task with acceptance criteria
- [x] #3 Decisions and rejected ideas recorded in this task's notes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Goal (Carl, 2026-10-19): tools that help fill 8 hours per day for the whole period. The app keeps track of when he worked on which issue and also registers the time on the Jira issues as worklogs. Issues are tasks today and are becoming stories; both must work.

Decision: missing hours are filled by suggestion. A 'Fill day' action proposes entries in the gaps, based on the Jira issues tied to Carl; he adjusts and accepts. Rejected: a single default issue, spreading evenly, warn-only.

Decision: 'tied to me' = issues assigned to Carl in the active sprint(s), not Done. Rejected: all assigned not-done issues; reported/watched/commented issues.

Decision: workdays are Mon-Fri at 8 h; Danish public holidays are skipped automatically; Carl can mark days off (vacation, sick). Rejected: holidays-only, weekdays-only.

Decision: workday window is a setting, default 08:00-16:30 with a 30 min unlogged lunch (default 12:00-12:30). Fill day only places blocks inside the window, outside lunch, in the gaps between existing entries. Rejected: window without lunch; day starting at first entry.

Decision: besides Fill day there is Fill period, which proposes blocks for every short workday in the period up to today; Carl reviews them in the grid and accepts all or per day. Rejected: day-by-day only.

Decision: issue choice per gap is a best guess: the issue logged right before or after the gap first, then sprint issues with status In Progress, then other sprint issues assigned to Carl. Every proposed block is editable. Rejected: In Progress only; empty blocks with chips.

Decision: entries go to Jira as worklogs on every save (create, update, delete right away); failures keep the entry locally as not synced with a retry. Fill proposals are drafts until accepted, so nothing reaches Jira before review. Rejected: submit per period, submit per day.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Planned with Carl. Resulting tasks, in build order: TASK-2 (worklog sync on save, rewritten), TASK-8 (workday settings, holidays, days off, missing hours), TASK-9 (active-sprint issues from Jira), TASK-10 (Fill day), TASK-11 (Fill period). TASK-3 (import existing worklogs) and TASK-4 (test on the real Jira) stay as they were.
<!-- SECTION:FINAL_SUMMARY:END -->
