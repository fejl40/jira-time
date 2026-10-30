---
id: TASK-93
title: Reject a Jira URL without a host on Connect
status: Done
assignee: []
created_date: '2026-10-30 13:01'
updated_date: '2026-10-30 13:01'
labels:
  - bug
dependencies: []
priority: low
ordinal: 158000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Typing only https:// (or http://) as the Jira URL on Connect becomes https://https: and is sent to Jira as if it were a site. JiraConnection.NormalizeUrl trims the trailing slashes before checking for a scheme, so the scheme is lost and added again. A URL without a host should get the usual 'That doesn't look like a valid URL.' message; a URL with a host behaves as before.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 https://, http:// and https:/// alone are rejected as not a valid URL
- [x] #2 A typed host keeps working as before: with or without scheme, trailing slashes trimmed, a path kept
- [x] #3 Only http and https URLs are accepted
- [x] #4 JiraConnectionTests cover the cases
- [x] #5 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
NormalizeUrl adds https:// before trimming trailing slashes, and accepts only http or https URLs with a host. https://, http://, https:/// and ftp:// URLs now get 'That doesn't look like a valid URL.'; typed hosts behave as before. Tests added to JiraConnectionTests.
<!-- SECTION:FINAL_SUMMARY:END -->
