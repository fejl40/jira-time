---
id: TASK-10
title: 'Fill day: suggest entries for a day''s missing hours'
status: Done
assignee: []
created_date: '2026-10-19 11:45'
updated_date: '2026-10-19 12:06'
labels: []
dependencies:
  - TASK-8
  - TASK-9
priority: high
type: feature
ordinal: 10000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A 'Fill day' button in the day view proposes entries for the gaps between existing entries, inside the workday window and outside lunch, until the day reaches 8 h. Issue per gap is a best guess: the issue logged right before or after the gap, then active-sprint issues In Progress, then other active-sprint issues assigned to the user. Proposals are drafts shown in the timeline; nothing is saved (and so nothing reaches Jira) until accepted. Each block can be changed or removed before accepting.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Proposals fill exactly the missing hours, only in gaps inside the workday window and outside lunch
- [x] #2 Issue choice follows the best-guess order above
- [x] #3 Proposals are visibly drafts and can be edited or removed one by one
- [x] #4 Accept saves all remaining proposals as entries (which then sync to Jira); cancel discards them
- [x] #5 Days off, holidays and weekends get no proposals
- [x] #6 Unit tests cover gap finding and issue choice
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Domain/DayFiller proposes draft entries for a workday's missing hours: free gaps inside the workday window, outside lunch and logged time, earliest first, cut off at 8 h. Issue per block: entry right before (lunch is transparent), then right after, then first In Progress sprint issue, then any sprint issue, else empty for the user to choose. Timesheet keeps proposals per day (ready for Fill period); the day view has a Fill day button for short workdays up to today, draws drafts dashed in the timeline, and DraftList lets each draft's issue and times be changed or removed. Accept saves through WorklogSync (so worklogs reach Jira); drafts that fail validation stay with the reason. Discard drops them; marking the day off discards too. DayFillerTests cover gaps, best-guess order, 8 h cut-off, custom windows and non-workdays; checked in the browser against the mock Jira.
<!-- SECTION:FINAL_SUMMARY:END -->
