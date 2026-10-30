namespace JiraTime.Tests.Components;

public class FaviconTests
{
    private static string App(params string[] path)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JiraTime.slnx"))) dir = dir.Parent;
        return Path.Combine([dir!.FullName, "src", "JiraTime", .. path]);
    }

    [Theory]
    [InlineData("favicon.svg", "rel=\"icon\" type=\"image/svg+xml\"")]
    [InlineData("apple-touch-icon.png", "rel=\"apple-touch-icon\"")]
    public void The_page_head_links_each_icon_and_the_file_exists(string file, string rel)
    {
        var head = File.ReadAllText(App("Components", "App.razor"));

        Assert.Contains($"<link {rel} href=\"@Assets[\"{file}\"]\" />", head);
        Assert.True(new FileInfo(App("wwwroot", file)).Length > 0);
    }

    [Fact]
    public void The_svg_icon_is_a_square_svg() =>
        Assert.Contains("viewBox=\"0 0 64 64\"", File.ReadAllText(App("wwwroot", "favicon.svg")));
}
