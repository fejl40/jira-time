---
id: TASK-102
title: Show the user's Jira profile picture in the header
status: To Do
assignee: []
created_date: '2026-10-30 14:08'
labels: []
dependencies: []
priority: low
ordinal: 167000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The header shows the user's initials. Jira's /myself, which sign-in already calls, returns avatarUrls (16, 24, 32 and 48 px) on Server/DC and Cloud. Show that picture in the header instead of the initials. On Server/DC the picture URL needs the token, which the browser can't send, so the server fetches the picture through JiraHttp (so the request goes through the rate limits and is logged in the request log, under the sign-in action or its own action) and serves it to the browser from an app URL. The browser keeps the picture for two days (Cache-Control max-age of 2 days, with a version in the URL so a new picture shows once Jira's URL changes), and the server reads it from Jira at most once per two days per user, so the picture costs few Jira calls. When Jira gives its default avatar, or the picture can't be read, the initials stay.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 The header shows the signed-in user's Jira profile picture (the 48 px one, shown small) instead of the initials, in light and dark
- [ ] #2 The picture is fetched by the server through JiraHttp with the user's token and every such request is in the Jira request log
- [ ] #3 The browser caches the picture for two days; the server reads it from Jira at most once per two days per user, and a changed avatar URL from Jira shows the new picture
- [ ] #4 A user only ever gets their own picture through the app URL
- [ ] #5 Jira's default avatar, a failed read or no avatar keeps the initials
- [ ] #6 Tests: the picture is read once and reused within two days, the request is logged, the default avatar and a failure keep the initials
- [ ] #7 Browser check against the mock Jira (with an avatar added to its /myself)
- [ ] #8 AGENTS.md describes it (header, request log, where the picture is kept)
- [ ] #9 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
