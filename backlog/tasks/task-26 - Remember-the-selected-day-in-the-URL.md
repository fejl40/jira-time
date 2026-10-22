---
id: TASK-26
title: Remember the selected day in the URL
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T19:48:00Z'
labels: []
dependencies: []
priority: medium
ordinal: 26000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Reload and back/forward should keep the selected day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Selecting a day puts it in the URL (?day=yyyy-mm-dd) without reloading the period
- [x] #2 Opening such a URL selects that day
- [x] #3 Today selects today even inside the current period
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Selecting a day navigates (replace) to period/{start}?day={date}; OnParametersSetAsync only switches the day when the period is unchanged, so nothing reloads and unsaved work stays. Opening such a URL selects the day, and Today (/) selects today inside the current period. Checked in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
