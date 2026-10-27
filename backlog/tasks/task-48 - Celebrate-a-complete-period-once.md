---
id: TASK-48
title: Celebrate a complete period once
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 11:11'
labels: []
dependencies: []
ordinal: 48000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: a small burst of confetti the first time a period becomes complete, never again for that period.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Confetti bursts when a period becomes complete
- [x] #2 Each period celebrates at most once in a browser
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
After each render the page checks whether the period became complete while shown (Milestones<Period>); if so jiraMotion.celebrate bursts 48 bits of confetti from the Complete so far badge for 600 ms. A localStorage key per user and period makes each period celebrate once per browser.
<!-- SECTION:FINAL_SUMMARY:END -->
