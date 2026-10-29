---
id: TASK-71.25
title: Replace the template Error page and remove leftovers
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:59'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 100000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Error.razor is the dotnet template with Bootstrap classes and developer text; ExportPage has its own Plural; _Imports has two unused usings. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Error.razor uses app styles, works in dark mode, no developer text
- [x] #2 Format.Count replaces Plural and is tested
- [x] #3 Unused usings removed; build passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Error.razor is a card like NotFound (Something went wrong, a link back, the request id as a hint); Format.Count replaces ExportPage.Plural and backs Format.Days; the unused Http and Forms usings are gone.
<!-- SECTION:FINAL_SUMMARY:END -->
