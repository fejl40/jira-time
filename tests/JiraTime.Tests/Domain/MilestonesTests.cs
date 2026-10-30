using JiraTime.Domain;

namespace JiraTime.Tests.Domain;

public class MilestonesTests
{
    [Fact]
    public void What_is_reached_at_the_first_look_does_not_count()
    {
        var m = new Milestones<int>();

        Assert.Empty(m.Update([1, 2]));
        Assert.Empty(m.Fresh);
    }

    [Fact]
    public void Newly_reached_things_stay_fresh_while_reached()
    {
        var m = new Milestones<int>();
        m.Update([1]);

        Assert.Equal([2], m.Update([1, 2]));
        Assert.Empty(m.Update([1, 2]));
        Assert.Equal([2], m.Fresh);

        m.Update([1]);
        Assert.Empty(m.Fresh);
        Assert.Equal([2], m.Update([1, 2])); // reached again
    }
}
