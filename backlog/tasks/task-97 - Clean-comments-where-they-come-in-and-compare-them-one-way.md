---
id: TASK-97
title: Clean comments where they come in and compare them one way
status: To Do
assignee: []
created_date: '2026-10-30 13:06'
labels: []
dependencies: []
priority: low
ordinal: 162000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Comments are compared in two ways: no comment and an empty one count as the same almost everywhere (TimeEntry.SameComment), but SameAsSynced, which decides whether a row matches what Jira has, compares character by character. Clean comments once where they come in instead: when Jira's worklogs are read (JiraWorklogJson / WorklogMatching.ToEntry) and when the user types one (SheetDay/DayWork SetComment), with TimeEntry.CleanComment (trimmed, blank becomes none). Then every comparison can use the one rule and SameAsSynced uses SameWork. Effect: a worklog whose comment differs from Jira's only by surrounding spaces counts as unchanged and is never sent again; when the user changes something else on such a row, the cleaned comment is sent with it.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Comments are cleaned when worklogs are read from Jira and when the user types a comment
- [ ] #2 SameAsSynced uses the same comment rule as every other comparison (SameWork)
- [ ] #3 Tests: a Jira worklog with spaces around its comment is unchanged after reading and isn't re-sent by Save all; changing its hours sends the cleaned comment
- [ ] #4 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
