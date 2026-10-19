---
id: TASK-2
title: Sync time entries to Jira as worklogs
status: Done
assignee: []
created_date: '2026-10-19 08:59'
updated_date: '2026-10-19 11:54'
labels: []
dependencies: []
modified_files:
  - src/JiraTime/Services/WorklogSync.cs
  - src/JiraTime/Services/JiraClient.cs
  - src/JiraTime/Services/TimeEntryService.cs
  - src/JiraTime/Domain/Documents.cs
  - src/JiraTime/Components/Shared/EntryEditor.razor
  - src/JiraTime/Components/Pages/Timesheet.razor
  - src/JiraTime/Program.cs
  - tests/JiraTime.Tests/FakeJira.cs
  - tests/JiraTime.Tests/WorklogSyncTests.cs
  - README.md
priority: high
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Send every time entry to Jira as a worklog the moment it is saved. Store the returned worklog id in TimeEntry.JiraWorklogId; editing an entry updates its worklog and deleting it removes the worklog. Works for tasks and stories. Server/DC (API v2, plain-text comment) and Cloud (API v3, ADF comment) use different worklog formats. If Jira is unreachable or rejects the call, the entry is still saved locally, marked as not synced, and can be retried.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Saving a new entry creates a worklog on its issue with the right start time, duration and comment
- [x] #2 Editing a synced entry updates its worklog, including moving it to another issue
- [x] #3 Deleting a synced entry deletes its worklog
- [x] #4 A failed sync keeps the entry, shows it as not synced, and offers a retry
- [x] #5 Entries show their sync state in the day view
- [x] #6 Covered by tests against a fake Jira
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
WorklogSync sends each entry to Jira on save: create, update (re-create if deleted in Jira), move to another issue (delete + create) and delete. Server/DC uses API v2 with a text comment, Cloud API v3 with an ADF comment; started is sent in Jira's format with the local UTC offset. Failures keep the entry with SyncPending + SyncError; the day view shows "In Jira" or "Not in Jira · Retry" with the reason, and the header offers "Sync now" for the period. A failed Jira delete keeps the entry. 12 new tests against an in-memory fake Jira; also checked in a browser with Jira going offline and coming back.
<!-- SECTION:FINAL_SUMMARY:END -->
