---
id: TASK-3
title: Import existing worklogs from Jira
status: Done
assignee: []
created_date: 2026-10-19 08:59
updated_date: '2026-10-22T19:42:00Z'
labels: []
dependencies: []
priority: high
ordinal: 3000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Fill the timesheet with worklogs already logged in Jira for the selected profile, so hours logged elsewhere are visible and not double counted.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Worklogs for the current period are imported and linked by worklog id
- [x] #2 Re-importing does not create duplicates
- [x] #3 Imported entries are marked From Jira and count in totals, missing hours and suggestions
- [x] #4 Import runs when a period opens and with Sync now; Jira being down only shows a notice
- [x] #5 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
JiraClient.GetMyWorklogsAsync searches issues with worklogAuthor + worklogDate in the range (paged, Cloud and Server), reads each issue's worklogs (paged) and keeps the user's own in the range; comments are read from text or Atlassian Document Format. WorklogSync.ImportAsync adds unknown worklogs as synced entries marked FromJira, updates synced entries Jira changed (issue, day, hours, comment), and removes synced entries whose worklog is gone after checking the worklog directly (search index lag can't drop work). Entries with pending local changes are left alone. The timesheet imports after rendering the local hours on period open and with Sync now, shows 'From Jira: …' or the error, and suggests only after the import. Day rows show '✓ From Jira'. WorklogImportTests cover import, no duplicates, own work, other authors and dates, Jira edits, pending local edits, deletes, index lag and comment formats.
<!-- SECTION:FINAL_SUMMARY:END -->
