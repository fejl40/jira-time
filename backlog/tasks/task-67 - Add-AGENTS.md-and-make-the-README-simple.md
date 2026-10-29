---
id: TASK-67
title: Add AGENTS.md and make the README simple
status: To Do
assignee: []
created_date: '2026-10-29 07:42'
labels:
  - docs
dependencies: []
priority: medium
ordinal: 71000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The README is cluttered with details meant for LLM agents. Move those (layout, conventions, how things work inside) to an AGENTS.md, and rewrite the README so a person understands it at a glance: what the app does, how to run it, where data lives.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 AGENTS.md holds the notes for agents: build and test, layout, conventions, internals
- [ ] #2 CLAUDE.md points to AGENTS.md instead of repeating it
- [ ] #3 README is short and plain: what it is, how to run it, where data lives, a link to AGENTS.md
<!-- AC:END -->
