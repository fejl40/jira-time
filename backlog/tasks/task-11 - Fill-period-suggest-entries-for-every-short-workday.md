---
id: TASK-11
title: 'Fill period: suggest entries for every short workday'
status: To Do
assignee: []
created_date: '2026-10-19 11:45'
labels: []
dependencies:
  - TASK-10
priority: medium
type: feature
ordinal: 11000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A 'Fill period' button runs Fill day for every workday in the period up to and including today that has fewer than 8 h. Proposals show in the grid as drafts; Carl reviews them and accepts all at once or day by day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Proposals are made for every short workday up to today, none for future days
- [ ] #2 The grid marks draft hours differently from saved hours
- [ ] #3 Accept all and accept per day both work; discard clears the drafts
- [ ] #4 After accepting all, every workday so far has 8 h (or is a day off/holiday)
<!-- AC:END -->
