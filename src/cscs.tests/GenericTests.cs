using csscript;
using CSScriptLib;
using Xunit;

public class GenericTests
{
    [Fact]
    public void SplittingArgsOnLinux()
    {
        var args = CLIExtensions.SplitMergedArgs(new[] { "-new", "test.cs" });
    }

    [Fact]
    public void EscapinginDirectives()
    {
        var parser = new CSharpParser(@"//css_ref C:\Program Files (x86)\dotnet\sdk\2.1.403\Microsoft.Build.dll;");
        // Assert.
    }
}