---
id: TASK-29
title: Configurable workday hours
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T19:39:00Z'
labels: []
dependencies: []
priority: low
ordinal: 29000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Hours per weekday are fixed at 8 Monday to Friday. Let each user set them, e.g. 37 hour weeks or short Fridays.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Settings has hours per weekday, saved per user
- [x] #2 Missing hours, suggestions and Fill period use them
- [x] #3 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
UserSettings document per user (user-settings collection) holds hours per weekday, default 8 h Monday to Friday. DanishHolidays.Classify takes the settings: a weekday with 0 hours is not a workday, a weekend day with hours is. DayInfo got Hours and Off; Expected = Hours - Off, so missing hours, suggestions and Fill period follow. Settings page has a Work hours card (7 inputs, weekly total, reset to standard, validation 0-24 h in whole minutes). Tests in WorkCalendarTests.
<!-- SECTION:FINAL_SUMMARY:END -->
