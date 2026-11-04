---
id: TASK-116.1
title: Rows fly into their days
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:22'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-116
priority: medium
ordinal: 195000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
As the modal closes after a successful save, a ghost of each saved day's change flies from its line in the modal to its day cell (FLIP with recorded positions).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Only days that saved fly; days that couldn't be saved don't
- [x] #2 Ends exactly on the cell, also when the grid scrolled
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
The Save all modal closes before Jira is asked, so the lines are copied when Save is clicked (positions recorded then) and wait where they were, breathing, until the save ends; then saved days fly and the rest fade. A copy follows its cell every frame and the grid scrolls sideways to the cell, so it lands exactly also when the page or grid scrolls.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
wwwroot/flight.js lifts each day's line from the Save all modal on Save, then flies the saved ones into their day cell ([data-day-cell], grid header or phone row) by transform, re-measuring the cell each frame; unsaved days' copies fade. Which days fly comes from SaveChoreography (tested).
<!-- SECTION:FINAL_SUMMARY:END -->
