---
id: TASK-87
title: Simple install steps in README
status: Done
assignee: []
created_date: '2026-10-30 10:16'
updated_date: '2026-10-30 10:18'
labels: []
dependencies:
  - TASK-86
ordinal: 136000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
README tells a colleague how to get Jira Time running locally in as few steps as possible: the .NET 10 SDK, clone, dotnet run, open the page, connect with a Personal Access Token. No Node.js. Keep it very simple.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 README's install section needs only the .NET 10 SDK and git
- [x] #2 It covers clone, run, open, and connecting with a token in a few short steps
- [x] #3 It says how to update to a newer version
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
README's Install section: .NET 10 SDK and git, clone, dotnet run, open localhost:5115, connect with a PAT (where to create one) or Cloud API token, git pull to update.
<!-- SECTION:FINAL_SUMMARY:END -->
