using TaskDev.Api.Domain.UseCases;
using Xunit;

namespace TaskDev.Tests;

public class GamificationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(250, 3)]
    [InlineData(4999, 50)]
    public void CalculateLevel_CorrectLevelFromXp(int xp, int expectedLevel)
    {
        var level = GamificationRules.CalculateLevel(xp);
        Assert.Equal(expectedLevel, level);
    }

    [Theory]
    [InlineData(1, "E")]
    [InlineData(2, "E")]
    [InlineData(3, "D")]
    [InlineData(5, "D")]
    [InlineData(6, "C")]
    [InlineData(11, "C")]
    [InlineData(12, "B")]
    [InlineData(20, "B")]
    [InlineData(21, "A")]
    [InlineData(34, "A")]
    [InlineData(35, "S")]
    [InlineData(100, "S")]
    public void CalculateRank_CorrectRankFromLevel(int level, string expectedRank)
    {
        var rank = GamificationRules.CalculateRank(level);
        Assert.Equal(expectedRank, rank);
    }

    [Theory]
    [InlineData("LOW", 15)]
    [InlineData("MEDIUM", 30)]
    [InlineData("HIGH", 50)]
    [InlineData("URGENT", 80)]
    public void GetXpForPriority_CorrectPoints(string priority, int expectedPoints)
    {
        var points = GamificationRules.GetXpForPriority(priority);
        Assert.Equal(expectedPoints, points);
    }

    [Fact]
    public void GetSubtaskXpFraction_TenSubtasks_SumEqualsTotal()
    {
        int totalXp = 100;
        int totalSubtasks = 10;
        int sum = 0;

        for (int i = 0; i < totalSubtasks; i++)
        {
            int fraction = GamificationRules.GetSubtaskXpFraction(totalXp, totalSubtasks, i);
            Assert.Equal(10, fraction);
            sum += fraction;
        }

        Assert.Equal(totalXp, sum);
    }

    [Fact]
    public void GetSubtaskXpFraction_OddSubtasks_SumEqualsTotalExactly()
    {
        int totalXp = 50; // HIGH priority
        int totalSubtasks = 3;
        int sum = 0;

        for (int i = 0; i < totalSubtasks; i++)
        {
            sum += GamificationRules.GetSubtaskXpFraction(totalXp, totalSubtasks, i);
        }

        Assert.Equal(50, sum);
    }
}
