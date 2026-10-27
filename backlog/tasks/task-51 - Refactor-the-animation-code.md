---
id: TASK-51
title: Refactor the animation code
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 11:23'
labels: []
dependencies: []
ordinal: 51000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
After the animation tasks: tidy the motion CSS and script, remove duplication between components, and keep the animation hooks simple.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Motion CSS and script have one clear structure with no leftovers
- [x] #2 Components use a small, consistent set of motion classes and hooks
- [x] #3 Build, tests and a browser check pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
motion.css now has one shared timing rule for all entrances plus one line per effect, the unused fast speed is used for colour fades, and the stagger lives with the other tokens. motion.js reads its speeds from the CSS (handling the minified .15s form, which had silently turned counting off), shares one helper for flying pieces, and clamps early frames that made counts dip below zero. A MotionScript service wraps every script call and ignores closed tabs and script errors, replacing three hand-written try/catch blocks. Checked in the browser, including with reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
