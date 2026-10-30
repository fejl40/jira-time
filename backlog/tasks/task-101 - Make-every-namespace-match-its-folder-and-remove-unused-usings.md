---
id: TASK-101
title: Make every namespace match its folder and remove unused usings
status: Done
assignee: []
created_date: '2026-10-30 13:59'
updated_date: '2026-10-30 14:11'
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
- [x] #1 Every .cs file under src/JiraTime and tests/JiraTime.Tests has the namespace of its folder (Program.cs excepted, as top-level statements)
- [x] #2 No Razor component sets @namespace to anything other than its folder's namespace
- [x] #3 No unused usings in .cs files, and no unused @using in _Imports.razor files
- [x] #4 The build reports a namespace that doesn't match its folder and an unused using as warnings (IDE0130, IDE0005)
- [x] #5 Behaviour and what the user sees stay the same
- [x] #6 doc-1 says namespaces match folders and the build checks it
- [x] #7 dotnet build has 0 warnings and dotnet test passes the same tests
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
The modal service class Modals lives in Components/Modals; with namespace JiraTime.Components.Modals, the name Modals would mean the namespace from every other component, so the class is renamed ModalService (file ModalService.cs). Components still inject it as Modals.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Every .cs file in src and tests now has its folder's namespace (Program.cs keeps top-level statements); the Modals components lost their @namespace. The modal service is renamed ModalService (injected as Modals), since a class can't share the Modals folder's name. Unused usings are removed, _Imports.razor lists only namespaces components use. .editorconfig makes IDE0130 (namespace matches folder) and IDE0005 (unused usings) warnings, and Directory.Build.props turns on EnforceCodeStyleInBuild with the XML documentation file IDE0005 needs (CS1591 and CS1573 off, matching the existing doc style), so the 0-warnings rule keeps both right; checked with a deliberately wrong namespace. Same 603 tests pass; the timesheet browser script gives the same output; picker and export checked. AGENTS.md and doc-1 updated.
<!-- SECTION:FINAL_SUMMARY:END -->
