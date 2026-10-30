---
id: TASK-84
title: Write the Jira request log without a byte order mark
status: To Do
assignee: []
created_date: '2026-10-30 10:06'
labels: []
dependencies: []
ordinal: 133000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Each JSONL log file starts with a UTF-8 BOM, so jq and Python's json reject its first line. Write plain UTF-8; the Download .jsonl keeps working.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 New log files have no byte order mark
- [ ] #2 Existing files with a BOM still read
- [ ] #3 A test checks the first bytes of a new file
<!-- AC:END -->
