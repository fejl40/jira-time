---
id: TASK-100
title: Write the export's hours with a decimal point
status: To Do
assignee: []
created_date: '2026-10-30 13:58'
labels: []
dependencies: []
priority: medium
ordinal: 165000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl decided the decimal comma in numbers was a mistake and should be reversed. The only place the app writes one is the month export: the Timer column is written as 7,5 and 0,25 (MonthExport, used by Copy, Download .tsv and the table on the Export page). Write hours with a decimal point instead (7.5, 0.25), the same as everywhere else in the app. Typing hours as 1,5 on the timesheet and in Settings stays accepted, since that only reads what the user types.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 The export's Timer column is written with a decimal point (7.5, 0.25, 8) in Copy, Download .tsv and the table on the Export page
- [ ] #2 No other number the app writes uses a decimal comma
- [ ] #3 Typing 1,5 in an hours field still reads as 1.5 hours
- [ ] #4 MonthExportTests cover the decimal point
- [ ] #5 AGENTS.md (Month export) says hours are written with a decimal point
- [ ] #6 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
