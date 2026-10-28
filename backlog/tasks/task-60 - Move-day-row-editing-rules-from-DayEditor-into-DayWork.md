---
id: TASK-60
title: Move day row editing rules from DayEditor into DayWork
status: To Do
assignee: []
created_date: '2026-10-28 07:13'
labels:
  - refactor
dependencies: []
priority: medium
ordinal: 60000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
DayEditor.razor holds editing rules: picking an issue, reading typed hours (bad hours kept), comments, adding a row, Fill the rest, Copy previous day (replacing untouched suggestions), removing and restoring rows. Move them into DayWork so the component only wires events and the rules get tests.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 DayWork has methods for each row edit and DayEditor calls them
- [ ] #2 Behaviour is unchanged
- [ ] #3 Tests cover the moved rules
<!-- AC:END -->
