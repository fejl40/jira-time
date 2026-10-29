---
id: TASK-69
title: Write the coding principles page
status: Done
assignee: []
created_date: '2026-10-29 07:46'
updated_date: '2026-10-29 07:47'
labels:
  - docs
dependencies: []
priority: high
ordinal: 73000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A page in the repo that says how code in Jira Time is written: readable for humans, few files in a logical structure, short files, clarity first, SOLID. Linked from AGENTS.md so agents follow it.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 PRINCIPLES.md describes structure, file size, naming, comments, SOLID, tests and UI code with concrete rules
- [x] #2 AGENTS.md links it and says it must be followed
- [x] #3 The rules fit the current codebase (limits based on what is there now)
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PRINCIPLES.md: layers and dependency direction, few files (one concept per file), size limits (150 aim, 250 max, ~30-line methods), clarity, SOLID with judgement, data and errors, tests, UI. Linked from AGENTS.md and README.
<!-- SECTION:FINAL_SUMMARY:END -->
