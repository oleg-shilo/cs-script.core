using CSScriptLib;
using Xunit;

public class GenericTests
{
    [Fact]
    public void SplittingArgsOnLinux()
    {
        var args = CLIExtensions.SplitMergedArgs(new[] { "-new", "test.cs" });
    }
}