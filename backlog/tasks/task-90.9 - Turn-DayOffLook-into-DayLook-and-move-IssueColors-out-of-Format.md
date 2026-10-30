---
id: TASK-90.9
title: Turn DayOffLook into DayLook and move IssueColors out of Format
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 12:09'
labels:
  - refactor
dependencies:
  - TASK-90.1
  - TASK-90.8
parent_task_id: TASK-90
priority: medium
ordinal: 148000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The weekend/holiday background and the 'holiday name or Weekend' text are written in PeriodGrid, DayList and DayHeader separately; DayHeader repeats the day-off badge and Undo twice; Format.cs holds IssueColors, a Tailwind colour picker. Rename DayOffLook to DayLook with Background, KindName and Title, use them in PeriodGrid, DayList and DayHeader, merge DayHeader's two time-off branches, and move IssueColors to Components/Sheet/IssueColors.cs.

Files:
- src/JiraTime/Components/DayOffLook.cs -> src/JiraTime/Components/DayLook.cs
- src/JiraTime/Components/Sheet/IssueColors.cs (new)
- src/JiraTime/Components/Format.cs
- src/JiraTime/Components/Sheet/PeriodGrid.razor
- src/JiraTime/Components/Sheet/DayList.razor
- src/JiraTime/Components/Sheet/DayHeader.razor
- src/JiraTime/Components/Sheet/TimeOffModal.razor
- src/JiraTime/Components/Sheet/DayRow.razor
- src/JiraTime/Components/Sheet/IssuePicker.razor
- src/JiraTime/Styles/app.css (comment only)
- tests/JiraTime.Tests/DayOffLookTests.cs -> tests/JiraTime.Tests/DayLookTests.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 "Weekend" and the weekend/holiday background classes appear only in DayLook
- [x] #2 DayHeader shows the time-off badge and Undo once
- [x] #3 Format.cs contains only text formatting
- [x] #4 DayLookTests cover Background, KindName and Title
- [x] #5 Rendered classes are unchanged
- [x] #6 Only the files listed under Files are created, edited, moved or deleted
- [x] #7 Behaviour and what the user sees stay the same
- [x] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
DayOffLook is now DayLook (Components/DayLook.cs) with Background (grey for weekends and holidays), KindName (holiday name, 'No work hours' or 'Weekend') and Title (the grid column's tooltip); Classes is renamed TimeOffClasses. PeriodGrid, DayList and DayHeader use them, so 'Weekend' and the weekend/holiday background are written only in DayLook. DayHeader shows the time-off badge and Undo once for whole and partial days off. IssueColors moved from Format.cs to Components/Sheet/IssueColors.cs (namespace JiraTime.Components, so DayRow and IssuePicker needed no change); Format.cs got summaries on Range and Iso. Tests: DayLookTests (was DayOffLookTests) add Background, KindName and Title. Doc changes for cleanup: AGENTS.md layout line 'Components/DayOffLook.cs' becomes 'Components/DayLook.cs  how a day looks: weekend and holiday grey and name, time off icon, tint and label per reason', add 'Components/Sheet/IssueColors.cs' (or mention IssueColors in the Components/Sheet/ line), and in 'Workdays' replace (DayOffLook, ...) with (DayLook, ...).
<!-- SECTION:FINAL_SUMMARY:END -->
