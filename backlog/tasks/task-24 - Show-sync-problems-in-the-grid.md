---
id: TASK-24
title: Show sync problems in the grid
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T20:01:00Z'
labels: []
dependencies: []
priority: medium
ordinal: 24000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Mark cells whose work is not in Jira so problems are visible without opening each day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 A cell with work that is not in Jira shows an amber marker with the reason on hover
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Grid cells with saved work that Jira doesn't have show an amber dot; the cell tooltip lists the sync errors. The narrow-screen day list shows the same marker per day. Checked in the browser with Jira down.
<!-- SECTION:FINAL_SUMMARY:END -->
