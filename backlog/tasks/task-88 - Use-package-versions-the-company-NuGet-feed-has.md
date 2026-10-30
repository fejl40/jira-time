---
id: TASK-88
title: Use package versions the company NuGet feed has
status: Done
assignee: []
created_date: '2026-10-30 10:58'
updated_date: '2026-10-30 10:59'
labels: []
dependencies: []
ordinal: 137000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl's company NuGet feed (shared-services-nuget) has Microsoft.Extensions.Http.Resilience up to 10.6.0 and Polly.Core up to 8.7.0, so the build fails with NU1102 on 10.11.0 and 8.9.0. Pin the app to versions that feed has, and keep all package versions at or below what is broadly mirrored.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Microsoft.Extensions.Http.Resilience is 10.6.0 and Polly.Core is 8.7.0 or lower
- [x] #2 dotnet build and dotnet test pass with those versions
- [x] #3 AGENTS.md says to pick package versions colleagues' feeds have
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Pinned Microsoft.Extensions.Http.Resilience 10.6.0 and Polly.Core 8.7.0 (still has BrokenCircuitException.RetryAfter); build and 471 tests pass. AGENTS.md notes the lagging company feed.
<!-- SECTION:FINAL_SUMMARY:END -->
