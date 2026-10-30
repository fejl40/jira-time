---
id: TASK-90.9
title: Turn DayOffLook into DayLook and move IssueColors out of Format
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
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
- [ ] #1 "Weekend" and the weekend/holiday background classes appear only in DayLook
- [ ] #2 DayHeader shows the time-off badge and Undo once
- [ ] #3 Format.cs contains only text formatting
- [ ] #4 DayLookTests cover Background, KindName and Title
- [ ] #5 Rendered classes are unchanged
- [ ] #6 Only the files listed under Files are created, edited, moved or deleted
- [ ] #7 Behaviour and what the user sees stay the same
- [ ] #8 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
