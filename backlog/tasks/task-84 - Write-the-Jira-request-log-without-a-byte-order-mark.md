---
id: TASK-84
title: Write the Jira request log without a byte order mark
status: Done
assignee: []
created_date: '2026-10-30 10:06'
updated_date: '2026-10-30 10:08'
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
- [x] #1 New log files have no byte order mark
- [x] #2 Existing files with a BOM still read
- [x] #3 A test checks the first bytes of a new file
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
The request log writes UTF-8 without a byte order mark, so jq and Python read every line. Older files with a BOM still read (the reader drops it), and the download no longer carries one. Tests check the first byte of a new file and reading an old one.
<!-- SECTION:FINAL_SUMMARY:END -->
