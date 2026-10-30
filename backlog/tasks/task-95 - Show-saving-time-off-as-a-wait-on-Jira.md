---
id: TASK-95
title: Show saving time off as a wait on Jira
status: To Do
assignee: []
created_date: '2026-10-30 13:06'
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
- [ ] #1 Marking and removing time off run as one wait in JiraWaits, from the start until the period is read again
- [ ] #2 While it runs, every button that sends a request to Jira (Sync, Save all, Try again, time off) is off and the day and grid show as pending, as with the other waits
- [ ] #3 The status text and spinner show it, also when the period reads back quickly
- [ ] #4 Tests cover time off as a wait (buttons wait, nothing else can start)
- [ ] #5 Browser check: time off and Undo show the wait and work as before
- [ ] #6 AGENTS.md Waiting for Jira lists time off with the others
- [ ] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
