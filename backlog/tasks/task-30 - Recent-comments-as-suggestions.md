---
id: TASK-30
title: Recent comments as suggestions
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T19:53:00Z'
labels: []
dependencies: []
priority: low
ordinal: 30000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Offer the comments used recently on the same issue when typing a comment.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The comment field suggests the latest distinct comments used on the row's issue
- [x] #2 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TimeEntryService.GetRecentCommentsAsync returns up to 5 distinct comments per issue from the last 60 days, newest first. The day editor renders a datalist per issue and links each comment field to its row's issue, so the browser suggests them while typing. Tests in TimeEntryServiceTests.
<!-- SECTION:FINAL_SUMMARY:END -->
