---
id: TASK-71.30
title: Remove duplicate focus logic in keys.js
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 105000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
focusHours and jiraKeys.focus duplicate focus and select; the hours selector is written twice. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Selector and focus code each appear once
- [ ] #2 Enter and focusing a new row still work
<!-- AC:END -->
