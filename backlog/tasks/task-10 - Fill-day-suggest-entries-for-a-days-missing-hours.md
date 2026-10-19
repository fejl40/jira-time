---
id: TASK-10
title: 'Fill day: suggest entries for a day''s missing hours'
status: To Do
assignee: []
created_date: '2026-10-19 11:45'
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
- [ ] #1 Proposals fill exactly the missing hours, only in gaps inside the workday window and outside lunch
- [ ] #2 Issue choice follows the best-guess order above
- [ ] #3 Proposals are visibly drafts and can be edited or removed one by one
- [ ] #4 Accept saves all remaining proposals as entries (which then sync to Jira); cancel discards them
- [ ] #5 Days off, holidays and weekends get no proposals
- [ ] #6 Unit tests cover gap finding and issue choice
<!-- AC:END -->
