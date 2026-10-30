---
id: TASK-76
title: Marking a whole day off removes its time from Jira on Save day
status: To Do
assignee: []
created_date: '2026-10-30 08:00'
labels:
  - feature
dependencies: []
priority: high
ordinal: 121000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl (2026-10-30): when a day is marked off, the day stays savable in Jira like normal, and the time previously saved on that day is removed from Jira. Today marking a whole day off throws away unsaved changes and leaves the worklogs in Jira. New behaviour: marking a whole day off keeps the editor and marks every saved row on the day as removed (struck through, Undo available) and drops suggestions; Save day then deletes those worklogs in Jira as usual. A partial day off keeps the logged time.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Marking a whole day off marks the day's saved rows as removed and keeps the user's other unsaved changes; nothing is written until Save day
- [ ] #2 Save day (and Save all) on a day off deletes the removed worklogs in Jira; the day stays editable
- [ ] #3 Undoing the day off before saving restores the rows; a partial day off leaves logged time alone
- [ ] #4 Tests in Domain and through TimeHarness; checked in the browser
<!-- AC:END -->
