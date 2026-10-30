---
id: TASK-89
title: >-
  Fetch several issues in one Jira search wherever the app still asks one at a
  time
status: Done
assignee: []
created_date: '2026-10-30 11:04'
updated_date: '2026-10-30 11:19'
labels: []
dependencies: []
ordinal: 138000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Jira's search takes key in (A-1, A-2, …) with fields=summary,worklog,… and returns up to a page of issues in one request. Use it everywhere the app still sends one request per issue or worklog. Writes stay one per worklog: Jira has no bulk worklog write.

- Recently saved issues that the worklog search doesn't find yet (RecentWorklogIssues) are read with one key-in search with fields=summary,worklog instead of one worklog GET each; only an issue whose embedded worklogs are incomplete is read on its own.
- The check before saving (WorklogSync.ChangedInJiraAsync) reads the issues of the worklogs it checks in one key-in search with their worklogs, instead of one GET per worklog. Save all does this once for every day it saves, before writing any day, instead of once per day.
- Key-in searches send validateQuery=warn, so an unknown or deleted key is skipped instead of failing the whole search; the one-key-at-a-time fallback in JiraClient.ByKeysAsync is only used if Jira still answers 400.
- Key-in searches page through results and send long key lists in batches (e.g. 100 keys), so more than 100 issues or epics are not cut off.
- POST /rest/api/2/search with a JSON body is used when the JQL would make the URL too long (longer than 2000 characters; 100 keys of a long project key are already over it).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Recently saved issues are read in one search with their worklogs, not one request per issue
- [x] #2 The changed-in-Jira check before saving uses one search for all checked issues, not one request per worklog
- [x] #3 Key-in searches use validateQuery=warn, and an unknown key no longer splits the search into one request per key
- [x] #4 Key-in searches page and batch, so more than 100 issues or epics are all read
- [x] #5 Tests count requests for each case: several recent issues, a save checking several worklogs, an unknown key, more than 100 keys
- [x] #6 Checked against the mock and noted in the task which parts need checking on Carl's real Jira
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Checked against the mock (scratchpad mock extended with validateQuery, POST search and paging), counting requests per action in the Jira request log. Save all on 3 days with 2 edited worklogs each: 13 requests before (6 worklog GETs, 6 PUTs, 1 read back), 8 after (1 key-in search, 6 PUTs, 1 read back). Open period right after a save while the worklog search lags: 3 before (search + 2 worklog GETs), 2 after (search + 1 key-in search). Export with one deleted issue among 5: 8 before (a 400, then one search per key), 3 after (1 search with validateQuery=warn). Export with 120 issues: before one GET search of 120 keys that returned only the first 100; after one POST of 100 keys and one GET of 20, all read.

Needs checking on Carl's real Jira (Data Center):
- validateQuery=warn on GET /rest/api/2/search (and in the POST body) really skips a deleted or unknown key with warningMessages instead of 400. If it still answers 400 the app falls back to one key at a time, as before.
- A key-in search with fields=worklog returns a just-written worklog right away (field values should come from the issue, not the worklogAuthor index). If not, a just-saved worklog disappears from the timesheet for a while; then the recent issues must go back to a worklog GET each.
- POST /rest/api/2/search with a JSON body (jql, startAt, maxResults, fields as a list, validateQuery) is accepted, and the URL limit in front of Jira is not below 2000 characters.
- How many worklogs the search embeds per issue (20 by default); an issue with more is still read on its own.
Cloud: /rest/api/3/search/jql has no validateQuery, so unknown keys still fall back to one key at a time there; POST /search/jql with nextPageToken is untested against real Cloud.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Several issues are now read in one Jira search wherever the app asked one at a time. A new Services/Jira/JiraSearch.cs is the one way to search: it reads every page (startAt/total on Server/DC, nextPageToken on Cloud), reads keys in batches of 100 that go out together, sends a search whose URL would pass 2000 characters as POST /search with a JSON body, and asks Server/DC to skip unknown keys with validateQuery=warn, keeping the key-by-key fallback only for a 400. Recently saved issues the worklog search doesn't find yet are read in one key-in search with their worklogs; the changed-in-Jira check reads the checked worklogs' issues in one search, and Save all does it once for all days. GetWorklogAsync is gone. FakeJira now honours validateQuery (strict by default), POST search and paging (search part split into FakeJiraSearch.cs). New tests count requests for recent issues, the check before saving (one day and Save all), unknown keys, more than 100 keys and epics, paging and POST.
<!-- SECTION:FINAL_SUMMARY:END -->
