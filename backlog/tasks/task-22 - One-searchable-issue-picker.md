---
id: TASK-22
title: One searchable issue picker
status: Done
assignee: []
created_date: '2026-10-22T19:35:00Z'
updated_date: '2026-10-22T19:53:00Z'
labels: []
dependencies: []
priority: high
ordinal: 22000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Replace the select with a Search Jira… option by one combobox: typing filters sprint and recent issues right away and also searches Jira.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Focusing the picker lists sprint and recent issues in groups
- [x] #2 Typing filters them by key or summary and adds Jira search results
- [x] #3 Arrow keys, Enter and Escape work
- [x] #4 The chosen issue shows its key and summary
- [x] #5 Covered by tests for the filtering
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
New IssuePicker combobox replaces the select with its Search Jira… option. On focus it selects the text and lists My sprint (with status) and Recent groups; typing filters them with IssueFilter (every word in key or summary, exact key then key prefix first) and from two characters adds a debounced Jira search group without duplicates. Arrow keys, Enter, Escape and blur (restores the chosen issue) work. IssueFilterTests cover the filtering; checked in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
