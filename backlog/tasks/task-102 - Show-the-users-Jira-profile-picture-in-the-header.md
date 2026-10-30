---
id: TASK-102
title: Show the user's Jira profile picture in the header
status: Done
assignee: []
created_date: '2026-10-30 14:08'
updated_date: '2026-10-30 14:20'
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
- [x] #1 The header shows the signed-in user's Jira profile picture (the 48 px one, shown small) instead of the initials, in light and dark
- [x] #2 The picture is fetched by the server through JiraHttp with the user's token and every such request is in the Jira request log
- [x] #3 The browser caches the picture for two days; the server reads it from Jira at most once per two days per user, and a changed avatar URL from Jira shows the new picture
- [x] #4 A user only ever gets their own picture through the app URL
- [x] #5 Jira's default avatar, a failed read or no avatar keeps the initials
- [x] #6 Tests: the picture is read once and reused within two days, the request is logged, the default avatar and a failure keep the initials
- [x] #7 Browser check against the mock Jira (with an avatar added to its /myself)
- [x] #8 AGENTS.md describes it (header, request log, where the picture is kept)
- [x] #9 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
The picture URL is profile-picture/{key}: the endpoint has no circuit and so no CurrentUser, so the key (128 random bits, new on every read from Jira) is the permission and the version; it is only written into its owner's pages. The token is only sent when the avatar is on the Jira host (never to Gravatar). Only PNG, JPEG, GIF and WebP are served (no SVG from the app's origin). The first read is logged under the sign-in/connect action it follows (the header renders inside it); later ones as Profile picture.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
The header shows the user's Jira profile picture (48 px from /myself avatarUrls) over the initials. ProfilePictures reads it through JiraHttp.GetPictureAsync (rate limits, request log; image bodies logged by type and size), keeps it per owner in profile-pictures, reads again after two days or when the avatar URL changes, and serves it at profile-picture/{random key} with Cache-Control private, max-age=172800, immutable. Jira's default avatar, failures, non-image answers and no avatar keep the initials. Tests in ProfilePictureTests and ProfilePicturesTests; browser check against the mock in light and dark.
<!-- SECTION:FINAL_SUMMARY:END -->
