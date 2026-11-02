---
id: TASK-111.2
title: Progress decides the aurora's colours
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-111
priority: medium
ordinal: 178000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A small rule maps the period's progress (logged up to today against what is needed up to today) to a palette stop: blue, teal, green, gold. Set as CSS custom properties on the layer so colours ease from one stop to the next.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Progress to palette is a tested rule (0%, partly, complete, Jira unknown = neutral)
- [ ] #2 Colours ease over about a second when progress changes
- [ ] #3 Pages without a period (Settings, Logs, Export) use the neutral palette
<!-- AC:END -->
