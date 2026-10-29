---
id: TASK-71.33
title: Share one fake issue search and TestClock in lookup tests
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 108000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Three classes write their own search handlers, JiraIssueLookup instances, JQL helpers and private clocks. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 No test class defines its own TimeProvider
- [ ] #2 Only FakeJira defines a search handler and Jql helper
- [ ] #3 Lookup tests use TimeHarness.Issues
<!-- AC:END -->
