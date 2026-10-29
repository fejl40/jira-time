---
id: TASK-66
title: Export a month of time in the agency's format
status: Done
assignee: []
created_date: '2026-10-29 07:34'
updated_date: '2026-10-29 07:43'
labels:
  - feature
dependencies: []
priority: high
ordinal: 66000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Export a calendar month (two periods) as tab-separated rows: Dato (yyyy-mm-dd), Timer, Fase (ydelsesområde), Forordning, Hovedleverance nr., Hovedleverance link, Epic, Beskrivelse. Decisions (Carl, 2026-10-29): one row per day and issue with hours added up; every day of the month gets a row, blank apart from the date when nothing is logged (changed from workdays only); Timer with a decimal comma; Fase, Forordning, Hovedleverance nr. and link are set once per epic in the app and remembered; Epic is a link to the issue's epic, empty when it has none, and then the per-epic columns are empty too; Beskrivelse is the issue key; a Copy button plus a .tsv download. Split into TASK-66.1 to TASK-66.4.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 A month page shows the export rows for a calendar month, with previous and next month
- [x] #2 Fase, Forordning, Hovedleverance nr. and link can be set per epic and are remembered per user
- [x] #3 Rows follow the decisions: every day of the month, blank when nothing is logged, one row per day and issue, decimal comma, epic link or empty, key as Beskrivelse
- [x] #4 Copy puts the rows (with header) on the clipboard and Download gives a .tsv file
- [x] #5 The issue's epic is read from Jira (parent epic, Epic Link field, or a sub-task's parent's epic)
- [x] #6 Tests cover the rows, the epic lookup and the per-epic values
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Done through TASK-66.1 to TASK-66.4.
<!-- SECTION:FINAL_SUMMARY:END -->
