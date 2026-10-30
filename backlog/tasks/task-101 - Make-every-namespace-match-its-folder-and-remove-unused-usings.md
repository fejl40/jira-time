---
id: TASK-101
title: Make every namespace match its folder and remove unused usings
status: To Do
assignee: []
created_date: '2026-10-30 13:59'
labels:
  - refactor
dependencies: []
priority: medium
ordinal: 166000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Namespaces don't follow the folders: 35 of 69 C# files in src/JiraTime (for example Services/Worklogs/TimeLog.cs is in JiraTime.Services) and all 65 test files (all in JiraTime.Tests, though they now sit in Domain, Services/..., Support and so on). Some Razor components also set @namespace to another folder's namespace (the Modals use JiraTime.Components). Give every C# file and Razor component the namespace of its folder (JiraTime.<Folder>.<Sub>, JiraTime.Tests.<Folder>.<Sub>), update the usings and _Imports.razor to match, and remove every unused using. Program.cs keeps its top-level statements (no namespace). Make the build enforce both from now on, so it stays this way: IDE0130 (namespace matches folder) and IDE0005 (unused usings) as warnings in an .editorconfig with EnforceCodeStyleInBuild, which the 0-warnings rule then keeps clean.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every .cs file under src/JiraTime and tests/JiraTime.Tests has the namespace of its folder (Program.cs excepted, as top-level statements)
- [ ] #2 No Razor component sets @namespace to anything other than its folder's namespace
- [ ] #3 No unused usings in .cs files, and no unused @using in _Imports.razor files
- [ ] #4 The build reports a namespace that doesn't match its folder and an unused using as warnings (IDE0130, IDE0005)
- [ ] #5 Behaviour and what the user sees stay the same
- [ ] #6 doc-1 says namespaces match folders and the build checks it
- [ ] #7 dotnet build has 0 warnings and dotnet test passes the same tests
<!-- AC:END -->
