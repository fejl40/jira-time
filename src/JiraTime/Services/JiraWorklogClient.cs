using System.Globalization;
using System.Net;
using JiraTime.Domain;
using static JiraTime.Services.JiraHttp;

namespace JiraTime.Services;

/// <summary>Jira REST calls that read and write worklogs. Runs on the server, so there are no CORS issues.</summary>
public class JiraWorklogClient(JiraHttp jira)
{
    private readonly JiraHttp _jira = jira;

    /// <summary>
    /// Worklogs <paramref name="profile"/> logged on days from <paramref name="from"/> to <paramref name="to"/>,
    /// on any issue, however they were logged. The issues in <paramref name="alsoRead"/> are read even when
    /// Jira's worklog search doesn't return them, which it can't for a while after a worklog was written.
    /// </summary>
    public async Task<IReadOnlyList<JiraWorklog>> GetMyWorklogsAsync(JiraConnection c, JiraProfile profile, DateOnly from, DateOnly to,
        IReadOnlyCollection<JiraIssue>? alsoRead = null, CancellationToken ct = default)
    {
        var jql = $"worklogAuthor = {Jql.User(profile)} AND worklogDate >= {Jql.Date(from)} AND worklogDate <= {Jql.Date(to)}";
        var found = await _jira.SearchAllAsync<SearchIssue>(c, jql, WorklogFields, ct);
        var keys = found.Select(i => i.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // A search by key reads the issues themselves, so their worklogs are there before the worklog search finds them.
        var notFound = (alsoRead ?? []).Where(i => keys.Add(i.Key)).Select(i => i.Key).ToList();
        if (notFound.Count > 0) found.AddRange(await _jira.ByKeysAsync<SearchIssue>(c, notFound, WorklogFields, ct));

        var range = new WorklogRange(profile, from, to);
        // Only an issue with more worklogs than the search sent is read on its own; those go out together.
        var perIssue = found.Select(async hit => range.Mine(await WorklogsOfAsync(c, hit, ct), ToIssue(hit)).ToList());
        return (await Task.WhenAll(perIssue)).SelectMany(w => w).ToList();
    }

    /// <summary>
    /// Every worklog on the given issues, by worklog id, read with the issues in one search (more when an issue has more
    /// worklogs than the search sends). Worklogs and issues Jira no longer has are left out.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, JiraWorklog>> GetWorklogsOnAsync(JiraConnection c, IReadOnlyCollection<string> issueKeys,
        CancellationToken ct = default)
    {
        var found = await _jira.ByKeysAsync<SearchIssue>(c, issueKeys, WorklogFields, ct);
        var perIssue = await Task.WhenAll(found.Select(async hit => (Issue: ToIssue(hit), Worklogs: await WorklogsOfAsync(c, hit, ct))));
        var result = new Dictionary<string, JiraWorklog>();
        foreach (var (issue, worklogs) in perIssue)
            foreach (var w in worklogs)
                if (w.Id is not null && ToWorklog(w, w.Id, issue) is { } worklog) result[worklog.Id] = worklog;
        return result;
    }

    private const string WorklogFields = "summary,worklog";

    private static JiraIssue ToIssue(SearchIssue hit) => new(hit.Key, hit.Fields?.Summary ?? "");

    /// <summary>The issue's worklogs from the search answer when it sent them all, else read from Jira; none when the issue is gone.</summary>
    private async Task<IReadOnlyList<RawJiraWorklog>> WorklogsOfAsync(JiraConnection c, SearchIssue hit, CancellationToken ct)
    {
        if (hit.Fields?.Worklog is { IsComplete: true } embedded) return embedded.Worklogs;
        try
        {
            return await ReadWorklogsAsync(c, hit.Key, ct);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    /// <summary>Every worklog of the issue, read page by page.</summary>
    private async Task<List<RawJiraWorklog>> ReadWorklogsAsync(JiraConnection c, string issueKey, CancellationToken ct)
    {
        var result = new List<RawJiraWorklog>();
        for (var startAt = 0; ; )
        {
            var page = await _jira.GetAsync<WorklogPage>(c, $"{WorklogUrl(c, issueKey)}?startAt={startAt}&maxResults=1000", ct);
            result.AddRange(page.Worklogs);
            startAt += page.Worklogs.Count;
            if (page.Worklogs.Count == 0 || startAt >= page.Total) break;
        }
        return result;
    }

    /// <summary>Whose worklogs are wanted, and on which days.</summary>
    private sealed record WorklogRange(JiraProfile Profile, DateOnly From, DateOnly To)
    {
        public IEnumerable<JiraWorklog> Mine(IEnumerable<RawJiraWorklog> worklogs, JiraIssue issue)
        {
            foreach (var w in worklogs)
            {
                if (w.Id is null || !IsAuthor(w.Author, Profile) || ToWorklog(w, w.Id, issue) is not { } worklog) continue;
                if (worklog.Date >= From && worklog.Date <= To) yield return worklog;
            }
        }
    }

    /// <summary>The worklog as the app keeps it; null when its day can't be read.</summary>
    private static JiraWorklog? ToWorklog(RawJiraWorklog w, string id, JiraIssue issue) =>
        ParseStarted(w.Started) is { } date
            ? new JiraWorklog(id, issue.Key, issue.Summary, date, WholeMinutes(w.TimeSpentSeconds), AtlassianDocument.ToPlainText(w.Comment), ParseCreated(w.Created))
            : null;

    /// <summary>The URL of the issue's worklogs, or of one of them.</summary>
    private static string WorklogUrl(JiraConnection c, string issueKey, string? worklogId = null) =>
        $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog" + (worklogId is null ? "" : "/" + Uri.EscapeDataString(worklogId));

    /// <summary>Worklog time in whole minutes (at least one), the unit entries are kept in.</summary>
    public static TimeSpan WholeMinutes(int seconds) => TimeSpan.FromMinutes(Math.Max(1, Math.Round(seconds / 60.0)));

    private static bool IsAuthor(RawUser? author, JiraProfile profile) =>
        author is not null && (author.AccountId == profile.Id || author.Key == profile.Id
                               || (profile.Username is not null && author.Name == profile.Username));

    /// <summary>The day of a worklog's "started" value, as Jira wrote it (2026-10-19T09:00:00.000+0200).</summary>
    public static DateOnly? ParseStarted(string? started) =>
        started is { Length: >= 10 } && DateOnly.TryParseExact(started[..10], "yyyy-MM-dd", out var d) ? d : null;

    /// <summary>A worklog's "created" value, e.g. 2026-10-19T09:00:00.000+0200; null when it can't be read.</summary>
    public static DateTimeOffset? ParseCreated(string? created)
    {
        if (created is not { Length: > 5 }) return null;
        // Jira writes the offset without a colon (+0200), which .NET only reads with one.
        var text = created[^5] is '+' or '-' ? created.Insert(created.Length - 2, ":") : created;
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    /// <summary>Creates a worklog for <paramref name="entry"/> on its issue.</summary>
    /// <returns>The new worklog id.</returns>
    public async Task<string> AddWorklogAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        var created = await _jira.SendAsync<RawWorklog>(c, HttpMethod.Post,
            $"{WorklogUrl(c, entry.IssueKey)}?adjustEstimate=auto", WorklogBody(c, entry), ct);
        return created?.Id ?? throw new JiraException("Jira did not return a worklog id.");
    }

    /// <summary>Overwrites an existing worklog on <paramref name="issueKey"/> with the entry's time and comment.</summary>
    public Task UpdateWorklogAsync(JiraConnection c, string issueKey, string worklogId, TimeEntry entry, CancellationToken ct = default) =>
        _jira.SendAsync<RawWorklog>(c, HttpMethod.Put,
            $"{WorklogUrl(c, issueKey, worklogId)}?adjustEstimate=auto", WorklogBody(c, entry), ct);

    /// <summary>Deletes a worklog on <paramref name="issueKey"/>.</summary>
    public Task DeleteWorklogAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct = default) =>
        _jira.SendAsync<object>(c, HttpMethod.Delete,
            $"{WorklogUrl(c, issueKey, worklogId)}?adjustEstimate=auto", null, ct);

    /// <summary>Jira's worklog "started" format, e.g. 2026-10-19T09:00:00.000+0200 (offset without a colon).</summary>
    public static string FormatStarted(DateOnly date, TimeOnly time, TimeZoneInfo? zone = null)
    {
        var local = date.ToDateTime(time);
        var offset = (zone ?? TimeZoneInfo.Local).GetUtcOffset(local);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        return $"{local:yyyy-MM-dd'T'HH:mm:ss}.000{sign}{Math.Abs(offset.Hours):00}{Math.Abs(offset.Minutes):00}";
    }

    private static object WorklogBody(JiraConnection c, TimeEntry e)
    {
        var started = FormatStarted(e.Date, TimeEntry.JiraStartTime);
        var seconds = (int)e.Duration.TotalSeconds;
        var text = e.Description ?? "";
        // Cloud (API v3) wants comments as Atlassian Document Format.
        object comment = c.IsCloud ? AtlassianDocument.FromPlainText(text) : text;
        return new { started, timeSpentSeconds = seconds, comment };
    }
}
