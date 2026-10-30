---
id: TASK-95
title: Show saving time off as a wait on Jira
status: Done
assignee: []
created_date: '2026-10-30 13:06'
updated_date: '2026-10-30 13:18'
labels: []
dependencies: []
priority: medium
ordinal: 160000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl decided that marking or removing time off goes through the same waiting state as the other actions, also when Jira isn't needed, because it can change worklogs (a whole day off marks the saved rows removed). Today only the reload inside it is a Jira wait; the local write before it is not, so another request can start meanwhile and the Time off button gets no spinner. Run the whole of Mark and Remove time off as one wait in JiraWaits, from the click until the period is read again.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Marking and removing time off run as one wait in JiraWaits, from the start until the period is read again
- [x] #2 While it runs, every button that sends a request to Jira (Sync, Save all, Try again, time off) is off and the day and grid show as pending, as with the other waits
- [x] #3 The status text and spinner show it, also when the period reads back quickly
- [x] #4 Tests cover time off as a wait (buttons wait, nothing else can start)
- [x] #5 Browser check: time off and Undo show the wait and work as before
- [x] #6 AGENTS.md Waiting for Jira lists time off with the others
- [x] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
AC #3: the reload inside time off usually reads from the cache and ends in ~10 ms, under the Waiting spinner's 200 ms delay, so nothing showed. Time off now holds its wait at least 600 ms (ShownPeriod.TimeOffShownFor, on the injected TimeProvider) so the spinner and status are seen. AC #6 (AGENTS.md) is left to the coordinator, who applies doc changes.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Mark and Remove time off now run as one JiraWait.TimeOff in JiraWaits from the click until the period is read again (at least 600 ms so the spinner shows): Sync, Save all, Try again and time off are off, the grid and day show pending, the header says 'Saving time off…' and the Time off…/Undo button gets the spinner. Time off doesn't start while another wait runs. Tests in JiraWaitsTests and ShownPeriodTests; checked in the browser with a slowed mock.
<!-- SECTION:FINAL_SUMMARY:END -->
