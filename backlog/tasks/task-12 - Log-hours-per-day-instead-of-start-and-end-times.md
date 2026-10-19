---
id: TASK-12
title: Log hours per day instead of start and end times
status: Done
assignee: []
created_date: '2026-10-19 12:22'
updated_date: '2026-10-19 12:27'
labels: []
dependencies: []
priority: high
type: enhancement
ordinal: 12000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl registers time per day: an entry is an issue, a day and a number of hours, plus an optional comment. When during the day the work happened does not matter; the goal is 8 h per workday. Remove start/end times, the workday window and lunch settings, overlap checks and the day timeline. Fill day and Fill period suggest hours per issue for the missing hours instead of time blocks. Worklogs sent to Jira still need a start time; use a fixed time on the day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 An entry has issue, date, hours and an optional description; no start or end time in the editor or the day view
- [x] #2 Existing entries with start/end are read as their duration
- [x] #3 Workday window and lunch settings are removed; workdays still need 8 h
- [x] #4 Fill day and Fill period suggest hours per issue for the missing hours, best-guess issue first
- [x] #5 Worklogs still sync to Jira with the entry's hours on its day
- [x] #6 Tests updated and cover the new entry rules and suggestions
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TimeEntry now stores Duration instead of Start/End. Files saved with start/end are read as their duration (legacy JSON properties) and rewritten without them on the next save. Validation: an issue is required, hours must be positive whole minutes, and a day can hold at most 24 h; the overlap check is gone. The editor and draft rows take hours as 1.5, 1,5 or 1:30 (Format.ParseHours), and a new entry defaults to the day's missing hours. The Workday window/lunch settings and the DayTimeline were removed; workdays still need 8 h (DayInfo.Target). DayFiller suggests one draft per short workday with the missing hours, on the issue last logged that day, then an In Progress sprint issue, then any sprint issue. Jira worklogs use 09:00 on the entry's day as the start time. Tests rewritten for the new rules, the parser, legacy files and suggestions (95 passing); checked in the browser against the mock Jira.
<!-- SECTION:FINAL_SUMMARY:END -->
