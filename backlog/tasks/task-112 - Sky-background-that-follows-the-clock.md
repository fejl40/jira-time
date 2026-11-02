---
id: TASK-112
title: Sky background that follows the clock
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 180000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
An alternative background: the sky at the user's time of day (their browser's time zone): pale dawn, bright day, orange dusk, night with faint twinkling stars. Dark mode always shows the night sky. Settings gets a Background choice (Aurora, Sky, Plain), kept per user.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Off (or still) when the system asks for reduced motion
- [ ] #2 Looks right in light and dark mode, on phones and wide screens
- [ ] #3 Rules that decide what shows are in Domain or a plain class and covered by tests
- [ ] #4 data-* attributes let a browser check find it; checked in the browser
- [ ] #5 Build has 0 warnings, tests pass, wwwroot/app.css committed with the styles
<!-- AC:END -->
