---
id: TASK-111.2
title: Progress decides the aurora's colours
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:15'
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
- [x] #1 Progress to palette is a tested rule (0%, partly, complete, Jira unknown = neutral)
- [x] #2 Colours ease over about a second when progress changes
- [x] #3 Pages without a period (Settings, Logs, Export) use the neutral palette
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Backdrop.Palette (Domain/Backdrop.cs) maps logged-vs-needed up to today (PeriodSummary.NeededSoFar, Missing) to Blue (<40%), Teal (<75%), Green, Gold (nothing missing); Neutral without a period, with Jira unknown or nothing needed yet. Timesheet publishes it through the scoped BackdropState; leaving the timesheet goes back to neutral. Colours are registered @property colours that ease over 1.2 s.
<!-- SECTION:FINAL_SUMMARY:END -->
