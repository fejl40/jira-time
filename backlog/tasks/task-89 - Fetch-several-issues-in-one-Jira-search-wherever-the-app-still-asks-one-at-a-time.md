---
id: TASK-89
title: >-
  Fetch several issues in one Jira search wherever the app still asks one at a
  time
status: To Do
assignee: []
created_date: '2026-10-30 11:04'
labels: []
dependencies: []
ordinal: 138000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Jira's search takes key in (A-1, A-2, …) with fields=summary,worklog,… and returns up to a page of issues in one request. Use it everywhere the app still sends one request per issue or worklog. Writes stay one per worklog: Jira has no bulk worklog write.

- Recently saved issues that the worklog search doesn't find yet (RecentWorklogIssues) are read with one key-in search with fields=summary,worklog instead of one worklog GET each; only an issue whose embedded worklogs are incomplete is read on its own.
- The check before saving (WorklogSync.ChangedInJiraAsync) reads the issues of the worklogs it checks in one key-in search with their worklogs, instead of one GET per worklog.
- Key-in searches send validateQuery=warn, so an unknown or deleted key is skipped instead of failing the whole search; the one-key-at-a-time fallback in JiraClient.ByKeysAsync is only used if Jira still answers 400.
- Key-in searches page through results and send long key lists in batches (e.g. 100 keys), so more than 100 issues or epics are not cut off.
- POST /rest/api/2/search with a JSON body is used when the JQL would make the URL too long.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Recently saved issues are read in one search with their worklogs, not one request per issue
- [ ] #2 The changed-in-Jira check before saving uses one search for all checked issues, not one request per worklog
- [ ] #3 Key-in searches use validateQuery=warn, and an unknown key no longer splits the search into one request per key
- [ ] #4 Key-in searches page and batch, so more than 100 issues or epics are all read
- [ ] #5 Tests count requests for each case: several recent issues, a save checking several worklogs, an unknown key, more than 100 keys
- [ ] #6 Checked against the mock and noted in the task which parts need checking on Carl's real Jira
<!-- AC:END -->
