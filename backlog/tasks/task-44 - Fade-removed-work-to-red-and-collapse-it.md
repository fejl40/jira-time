---
id: TASK-44
title: Fade removed work to red and collapse it
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 11:06'
labels: []
dependencies: []
ordinal: 44000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: removed rows fade to red; rows that go away collapse, and the day total counts down.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Removing a row fades it to red
- [x] #2 Removed new rows and saved removals collapse before they disappear
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Editor rows are keyed by entry id and fade their colours (motion-colors), so a removed row turns red and the day total counts down. jiraMotion.collapse shrinks rows away: a removed new row before it leaves, and removed saved rows after Save day or Save all succeed.
<!-- SECTION:FINAL_SUMMARY:END -->
