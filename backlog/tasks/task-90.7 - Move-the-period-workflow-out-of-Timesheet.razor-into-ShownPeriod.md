---
id: TASK-90.7
title: Move the period workflow out of Timesheet.razor into ShownPeriod
status: Done
assignee: []
created_date: '2026-10-30 11:32'
updated_date: '2026-10-30 12:05'
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
- [x] #1 Timesheet.razor's @code is about 60 lines or fewer and the file is <= 150 lines
- [x] #2 DayPanel does not inject WorkCalendarService and has no OnReload
- [x] #3 ShownPeriodTests cover: Save all selects the first day that can't be saved and sets the notice; a whole day off marks saved rows removed after reload; a stale period is not shown
- [x] #4 Only the files listed under Files are created, edited, moved or deleted
- [x] #5 Behaviour and what the user sees stay the same
- [x] #6 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Added Components/Sheet/ShownPeriod.cs (registered transient in Program.cs, one per timesheet page): it owns the PeriodSheet, PeriodExtras, the unsynced count, JiraWaits, the notice, the slide direction, SaveMotion (moved here from Motion.cs) and the shown day, with ShowAsync(link) (open a period or show another day of it; a period left before it was read is not shown), ReloadAsync, RetryAsync, SyncAsync, SaveDaysAsync(beforeReload) (returns the first day to fix and sets Notice), DiscardAll, LeaveWarning(target period) and Mark/UnmarkDayOffAsync. Timesheet.razor (118 lines, @code 75 lines with blanks, 62 without) keeps the URL and SelectDay, the Save all and leave modals, focus (DayKeys) and the motion calls; it no longer injects TimeLog, PeriodLoader, IssueStatuses, WorklogSync or TimeProvider. DayPanel takes Shown + Day + OnChanged, has no WorkCalendarService or OnReload, and calls ShownPeriod for time off. JiraWaits.cs moved to Components/Sheet/ (namespace JiraTime.Components). ShownPeriodTests (7) cover Save all choosing the day to fix with its notice, a whole day off and its undo, a stale period, another day of the period, slide direction and the leave warning. Browser check against the mock (fill, Save all, notice, time off and undo, leave confirm, cell click, Jira down and Try again, Sync) gave the same output before and after the change. Doc changes for cleanup (AGENTS.md Layout): Components/Pages/Timesheet.razor: 'shows a period: URL and selected day, modals, focus and animations around ShownPeriod'; add 'Components/Sheet/ShownPeriod.cs  the period on screen and its work with Jira: open, reload, Try again, Sync now, Save all, time off; SaveMotion'; move 'Components/JiraWaits.cs' into the Components/Sheet/ line (JiraWaits) and drop SaveMotion from the Components/Motion.cs line. Pre-existing, unchanged: when Save all's first day to fix is not the shown day, navigating to it clears the notice right away (ShowDay clears it on the URL change), and a period reached through the leave confirmation shows no slide class.
<!-- SECTION:FINAL_SUMMARY:END -->
