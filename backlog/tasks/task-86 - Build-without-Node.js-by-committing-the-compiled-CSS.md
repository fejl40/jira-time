---
id: TASK-86
title: Build without Node.js by committing the compiled CSS
status: Done
assignee: []
created_date: '2026-10-30 10:16'
updated_date: '2026-10-30 10:17'
labels: []
dependencies: []
ordinal: 135000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Colleagues have the .NET 10 SDK but not necessarily Node.js. Commit the compiled Tailwind output (src/JiraTime/wwwroot/app.css) and only run npm install and the Tailwind build when Node.js is installed, so dotnet build and dotnet run work with just the .NET SDK. Developers who change styles rebuild app.css with Node and commit it.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 wwwroot/app.css is committed and no longer git-ignored
- [x] #2 dotnet build and dotnet run work on a machine without Node.js, using the committed CSS
- [x] #3 With Node.js installed the build still compiles Tailwind as before
- [x] #4 AGENTS.md says to commit app.css after changing styles
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
wwwroot/app.css is committed; the csproj checks for Node.js and only runs npm install and the Tailwind build when it is there, otherwise uses the committed CSS. Checked: a clean clone builds and serves the CSS with Node removed from PATH.
<!-- SECTION:FINAL_SUMMARY:END -->
