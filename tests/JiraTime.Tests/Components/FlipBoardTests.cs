using JiraTime.Components;

namespace JiraTime.Tests.Components;

public class FlipBoardTests
{
    private static string Turning(string? before, string now) =>
        string.Concat(FlipBoard.Between(before, now).Select(t => t.Flips ? '^' : '_'));

    [Fact]
    public void Only_changed_digits_flip()
    {
        Assert.Equal("_^", Turning("32", "38"));
        Assert.Equal("^^", Turning("32", "40"));
        Assert.Equal("__", Turning("32", "32"));
    }

    [Fact]
    public void Numbers_line_up_on_the_decimal_point()
    {
        var tiles = FlipBoard.Between("7.5", "15.5");

        Assert.Equal("15.5", string.Concat(tiles.Select(t => t.Now)));
        Assert.Equal(" 7.5", string.Concat(tiles.Select(t => t.Before)));
        Assert.Equal([1, 0, FlipTile.PointPlace, -2], tiles.Select(t => t.Place));
        Assert.Equal("^^__", Turning("7.5", "15.5"));
    }

    [Fact]
    public void Decimals_that_come_or_go_flip_in_from_blank()
    {
        Assert.Equal("_^^", Turning("8", "8.5"));
        Assert.Equal("__^^", Turning("7.5", "7.25"));
        Assert.Equal("^", Turning("7.5", "8"));
        Assert.Equal(" ", string.Concat(FlipBoard.Between("", "8").Select(t => t.Before)));
    }

    [Fact]
    public void Nothing_flips_the_first_time_a_number_shows() =>
        Assert.Equal("____", Turning(null, "15.5"));

    [Fact]
    public void The_point_is_marked() =>
        Assert.Equal([false, true, false], FlipBoard.Between(null, "7.5").Select(t => t.IsPoint));
}
