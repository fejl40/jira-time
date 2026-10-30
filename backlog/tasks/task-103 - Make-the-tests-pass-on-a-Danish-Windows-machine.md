---
id: TASK-103
title: Make the tests pass on a Danish Windows machine
status: Done
assignee: []
created_date: '2026-10-30 15:02'
updated_date: '2026-10-30 15:04'
labels: []
dependencies: []
priority: high
ordinal: 168000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
On Carl's Windows PC (Danish number format) 5 DayFillerTests fail: test helpers write hours with the machine's culture (2,5h instead of 2.5h). The app itself runs with the invariant culture (Program.cs, TASK-100); the tests should run the same way, whatever the machine's language settings. Running the suite under da-DK on Linux also showed that a worklog's start time was written with the culture's time separator (09.00.00), so Jira's formats must be invariant in the code itself.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The test assembly runs with the invariant culture, like the app
- [x] #2 The whole suite passes when the machine's culture is Danish (da-DK)
- [x] #3 Build has 0 warnings and all tests pass
- [x] #4 Jira's worklog start time is written and read with the invariant culture, tested under da-DK and fi-FI
<!-- AC:END -->
