---
id: TASK-75.3
title: Add the Jira logs tab
status: Done
assignee: []
created_date: '2026-10-30 07:50'
updated_date: '2026-10-30 08:33'
labels:
  - feature
dependencies:
  - TASK-75.1
  - TASK-75.2
  - TASK-75.4
  - TASK-75.5
parent_task_id: TASK-75
priority: medium
ordinal: 118000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A Logs page in the top bar showing the signed-in user's Jira log, newest first, grouped by user action. Each request is one row with method, path, status, duration and time; a row expands into the detail view from TASK-75.4. Filters: day, errors only, text search. A Download button for the day's .jsonl and a Clear logs button. Big days stay fast (read lazily or page).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Logs tab in the top bar lists requests grouped by user action, newest first, with status colours
- [x] #2 Each request row expands and collapses in place to show the TASK-75.4 detail view
- [x] #3 Filter by day, errors only and text; download the day's .jsonl; clear logs
- [x] #4 Users only ever see their own logs (tested)
- [x] #5 Light and dark mode; checked in the browser against the mock Jira
- [x] #6 While the tab is open, new requests appear live and in-progress ones update when they finish
- [x] #7 A link from a toast opens the tab with that request expanded
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Live updates (Carl, 2026-10-30): the Logs tab subscribes to the TASK-75.1 feed while open. New requests appear at once in their action group, requests in progress show a spinner and update when they finish. Opening a request from a toast (TASK-75.5) scrolls to and expands it.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Logs in the top bar (/logs, Components/Pages/LogsPage) shows the signed-in user's requests for a day through MyJiraLog (everything keyed by CurrentUser.Owner): newest first, grouped by user action (Domain/JiraLogDay; neighbouring Background requests share a group), status pills coloured, rows expand in place into RequestDetails. Filters: day (days with a log), errors only, text (action, URL, status, error, bodies). Download .jsonl streams the day's file (wwwroot/logs.js), Clear logs deletes the user's files after a confirm. 40 actions render at first with Show more, and bodies are parsed only when a row opens. While open it subscribes to the feed: new requests appear at once with a spinner and update when they finish. A toast link (logs?day=…&request=…) expands that request, pages to it and scrolls it into view. The header nav now scrolls sideways on phones instead of widening the page (it already overflowed at 390 px before the Logs link). Checked in Chromium against the mock Jira: toasts (order, spinner, fade, errors staying and closing, Save all burst with +10 more, click to Logs), Logs (grouping, keyboard expand, JSON tree fold/Expand all/Collapse all/Copy, filters, download, live update from another tab), light and dark, 390 px phone (no horizontal scroll, toasts clear of Save day). Tests: JiraLogDayTests, MyJiraLogTests.
<!-- SECTION:FINAL_SUMMARY:END -->
