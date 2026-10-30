---
id: TASK-90.7
title: Move the period workflow out of Timesheet.razor into ShownPeriod
status: To Do
assignee: []
created_date: '2026-10-30 11:32'
labels:
  - refactor
dependencies:
  - TASK-90.1
parent_task_id: TASK-90
priority: high
ordinal: 146000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Timesheet.razor's @code block is about 200 lines: URL and day, loading, retry, Save all, sync, leave confirmation, focus and animations; DayPanel decides the time-off flow around WorkCalendarService and calls back into the page with OnReload. Create Components/Sheet/ShownPeriod.cs owning the sheet, extras, waits, notice and save motion with OpenAsync, ReloadAsync, RetryAsync, SyncAsync, SaveDaysAsync and Mark/UnmarkDayOffAsync. Timesheet.razor keeps URL and day selection, modals, focus and motion calls. DayPanel drops WorkCalendarService and OnReload. Move JiraWaits.cs to Components/Sheet/ and SaveMotion next to ShownPeriod (namespace stays JiraTime.Components).

Files:
- src/JiraTime/Components/Sheet/ShownPeriod.cs (new)
- src/JiraTime/Components/Pages/Timesheet.razor
- src/JiraTime/Components/Sheet/DayPanel.razor
- src/JiraTime/Components/Motion.cs
- src/JiraTime/Components/JiraWaits.cs -> src/JiraTime/Components/Sheet/JiraWaits.cs
- src/JiraTime/Program.cs (register ShownPeriod if needed)
- tests/JiraTime.Tests/ShownPeriodTests.cs (new)
- tests/JiraTime.Tests/MotionTests.cs
- tests/JiraTime.Tests/JiraWaitsTests.cs

Rules for this subtask: edit only the files listed. Do not edit AGENTS.md, README.md, backlog docs or wwwroot/app.css; write any doc changes needed into the final summary so the Post-refactor cleanup applies them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Timesheet.razor's @code is about 60 lines or fewer and the file is <= 150 lines
- [ ] #2 DayPanel does not inject WorkCalendarService and has no OnReload
- [ ] #3 ShownPeriodTests cover: Save all selects the first day that can't be saved and sets the notice; a whole day off marks saved rows removed after reload; a stale period is not shown
- [ ] #4 Only the files listed under Files are created, edited, moved or deleted
- [ ] #5 Behaviour and what the user sees stay the same
- [ ] #6 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
