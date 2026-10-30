---
id: doc-1
title: Coding principles
type: guide
created_date: '2026-10-29 08:33'
---



The goal: anyone can open any file in Jira Time and understand it in a few minutes.
When two options both work, pick the one a newcomer would understand faster.

## Structure

The app has four layers, each in its own folder under `src/JiraTime/`:

| Folder | Holds | May use |
|---|---|---|
| `Domain/` | The rules: periods, days, entries, fill suggestions, the export. Plain C#, no I/O, no Blazor. | Only `IDocument` and `[DocumentCollection]` from `Storage`, to mark what is stored |
| `Services/` | Talking to Jira, the document store and the signed-in user. All I/O lives here. | `Domain`, `Storage` |
| `Storage/` | The document store interface and its file implementation. | Nothing else in the app |
| `Components/` | The UI: `Pages/`, `Sheet/` (the timesheet parts), `Shared/`, `Layout/`. | `Services`, `Domain` |

- Dependencies point one way: Components → Services → Domain. Domain never knows about Jira, files or the UI.
- **Keep the number of files low.** A file holds one concept, not one type. Small types that belong to a concept (a result record, an enum, an options class) live in the same file as the class that uses them.
- Make a new folder only when at least three files belong together.
- Small browser helpers go in `wwwroot/` as one script per feature. Styles go in `Styles/`.
- Tests follow the same concepts: `tests/JiraTime.Tests/<Thing>Tests.cs`.

## Size

- **Files:** aim for 150 lines or fewer. 250 lines is the limit. A file that grows past it is split by responsibility, never just to cut its length.
- **Methods:** fit on one screen, about 30 lines. Four parameters at most; above that, pass a record.
- **Razor:** when the `@code` block grows past about 60 lines, move the rules into a Domain class (where they can be tested) or move part of the page into a child component.

## Clarity

- Names use the words a user would say: period, day, worklog, epic, day off. No abbreviations, and no `Manager`, `Helper` or `Util`.
- Write straight-line code. Return early, and give intermediate values a name instead of building long LINQ chains.
- Reuse what exists (`Format`, `TypedHours`, `Links`, `ExpiringCache`, `TimeHarness`) before writing something new. There is one way to do each thing.
- Delete dead code. Never leave code commented out.
- Every public type gets a one-line summary. Comments explain *why*; the code itself shows *what*.
- Text the user sees is written as plain, friendly sentences.

## SOLID, applied with judgement

- **Single responsibility:** a class has one reason to change. For example, `JiraClient` reads issues while `JiraWorklogClient` handles worklogs, and `PeriodSheet` holds the rules while `Timesheet.razor` only wires them up.
- **Open/closed:** add behaviour with a new type, such as a new `IDocumentStore`, rather than a flag in an existing one.
- **Liskov:** an implementation keeps the promises its interface makes.
- **Interface segregation:** keep interfaces small, like `IDocumentStore` and `IDocumentCollection<T>`.
- **Dependency inversion:** inject services and `TimeProvider`. Logic never calls `DateTime.Now` or creates an `HttpClient` itself.
- Do not add an interface or an abstraction that has only one implementation and no test need for it. Simple beats ceremonial.

## Data and errors

- Jira is the source of truth for logged time. Local files hold only what Jira can't: days off, settings, export values, and the retry queue.
- Jira failures surface as `JiraException`. Catch them at the edge (a page or a service) and show a message the user can act on.
- Never lose what the user typed. Unsaved changes stay until they are saved or discarded, and failed writes wait in the retry queue.

## Tests

- Every feature and every bug fix comes with tests. Build and tests pass before every push.
- Test Domain classes directly. Test Services through `TimeHarness` and `FakeJira`; tests never touch the network.
- Test names are sentences: `Time_on_the_same_issue_and_day_is_added_up`.
- Build entries and sheets with `TestEntries` and `TestSheets` (`Entries.cs`). A class tested in more than one file says why in its summary (the partial `DayWorkTests`, the `JiraIssueLookup*Tests`).
- Documents that belong to a user are read and written through `OwnedCollection<T>`, never by filtering on `Owner` by hand.

## UI

- Put Tailwind classes in the markup. Styles used in several places become named classes in `Styles/app.css` (`btn-primary`, `card`, `field`).
- Every element works in both light and dark mode.
- Give elements that browser checks need a `data-*` attribute, so a style change doesn't break the checks.
