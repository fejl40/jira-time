---
id: TASK-62
title: Read app links in one place
status: Done
assignee: []
created_date: '2026-10-28 07:13'
updated_date: '2026-10-28 07:15'
labels:
  - refactor
dependencies: []
priority: low
ordinal: 62000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
MainLayout (is this the timesheet?) and the timesheet's leave guard (which period does a link go to?) each parse URLs. Links should both build and read timesheet links, with tests.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Links reads a relative URL into the period and day it shows, or none
- [x] #2 MainLayout and the leave guard use it
- [x] #3 Tests cover the parsing
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Links.Read turns an app-relative URL into the timesheet period and day it shows (or null for other pages); MainLayout and the leave guard use it, and Format.ParseIso replaces the page's own date parsing.
<!-- SECTION:FINAL_SUMMARY:END -->
