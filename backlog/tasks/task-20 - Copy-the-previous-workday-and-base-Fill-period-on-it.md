---
id: TASK-20
title: Copy the previous workday and base Fill period on it
status: To Do
assignee: []
created_date: '2026-10-22T19:35:00Z'
labels: []
dependencies: []
priority: high
ordinal: 20000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Most days look like the day before. Offer Copy previous workday in the day editor (same issues and split, scaled to the missing hours), and let the automatic suggestion and Fill period copy the previous worked day on an empty day instead of putting everything on one sprint issue.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Copy previous workday adds the previous worked day's issues scaled to the missing hours, in quarter hours
- [ ] #2 An empty short day is suggested as a copy of the previous worked day, when there is one in the last two weeks
- [ ] #3 Fill period chains: each empty day copies the day before it, including suggested days
- [ ] #4 A partly logged day still gets the rest on the best-guess issue
- [ ] #5 Covered by tests
<!-- AC:END -->
