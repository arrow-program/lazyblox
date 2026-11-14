namespace LazyBlox.Tests;

public class BrowserCommandTests
{
    private const string Chrome = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    [Theory]
    [InlineData("\"C:\\a b\\x.exe\" --flag %1", new[] { @"C:\a b\x.exe", "--flag", "%1" })]
    [InlineData("C:\\x.exe   -a\t-b", new[] { @"C:\x.exe", "-a", "-b" })]
    [InlineData("\"C:\\x.exe\" \"a \\\"q\\\" b\"", new[] { @"C:\x.exe", "a \"q\" b" })]
    [InlineData("\"C:\\x.exe\" \"\"", new[] { @"C:\x.exe", "" })]
    [InlineData("x.exe a\\\\\\\"b", new[] { "x.exe", "a\\\"b" })]
    [InlineData("x.exe \"a\\\\\" b", new[] { "x.exe", "a\\", "b" })]
    public void Tokenize_FollowsCommandLineToArgvRules(string cmd, string[] expected) =>
        Assert.Equal(expected, BrowserCommand.Tokenize(cmd));

    [Fact]
    public void Chrome_SingleArgument()
    {
        Assert.True(BrowserCommand.TryBuild($"\"{Chrome}\" --single-argument %1", "https://example.com/?a=1&b=2", null, out var inv, out _));
        Assert.Equal(Chrome, inv!.FileName);
        Assert.Equal(["--single-argument", "https://example.com/?a=1&b=2"], inv.Arguments);
    }

    [Fact]
    public void Firefox_QuotedPlaceholder()
    {
        Assert.True(BrowserCommand.TryBuild("\"C:\\Program Files\\Mozilla Firefox\\firefox.exe\" -osint -url \"%1\"", "https://e.com/", null, out var inv, out _));
        Assert.Equal(["-osint", "-url", "https://e.com/"], inv!.Arguments);
    }

    [Fact]
    public void NoPlaceholder_AppendsUrl()
    {
        Assert.True(BrowserCommand.TryBuild($"\"{Chrome}\"", "https://e.com/", null, out var inv, out _));
        Assert.Equal(["https://e.com/"], inv!.Arguments);
    }

    [Fact]
    public void PercentStar_IsDropped_AndLowerL_Works()
    {
        Assert.True(BrowserCommand.TryBuild($"\"{Chrome}\" %l %*", "https://e.com/", null, out var inv, out _));
        Assert.Equal(["https://e.com/"], inv!.Arguments);
    }

    [Theory]
    [InlineData("https://e.com/?x=\" --evil")]
    [InlineData("https://e.com/ & calc.exe")]
    [InlineData("https://e.com/?q=%PATH%")]
    public void HostileUrl_StaysOneArgument_AndIsNotEnvExpanded(string url)
    {
        Assert.True(BrowserCommand.TryBuild($"\"{Chrome}\" --single-argument %1", url, null, out var inv, out _));
        Assert.Equal(2, inv!.Arguments.Count);
        Assert.Equal(url, inv.Arguments[1]);
    }

    [Fact]
    public void EnvVars_ExpandedInExecutableOnly()
    {
        Assert.True(BrowserCommand.TryBuild("\"%SystemRoot%\\notepad.exe\" %1", "https://e.com/%SystemRoot%", null, out var inv, out _));
        Assert.Equal(Path.Combine(Environment.GetEnvironmentVariable("SystemRoot")!, "notepad.exe"), inv!.FileName);
        Assert.Equal("https://e.com/%SystemRoot%", inv.Arguments[0]);
    }

    [Theory]
    [InlineData("chrome.exe %1")]                 // relative
    [InlineData("")]
    [InlineData("   ")]
    public void Invalid_Templates(string template) =>
        Assert.False(BrowserCommand.TryBuild(template, "https://e.com/", null, out _, out _));

    // ---- 9. loop prevention ----
    [Theory]
    [InlineData("\"C:\\Users\\x\\AppData\\Local\\Programs\\LazyBlox\\LazyBlox.exe\" \"%1\"")]
    [InlineData("\"D:\\anywhere\\lazyblox.EXE\" %1")]
    [InlineData("rundll32.exe url.dll,FileProtocolHandler %1")]
    [InlineData("\"C:\\Windows\\System32\\rundll32.exe\" url.dll,FileProtocolHandler %1")]
    [InlineData("\"C:\\Windows\\explorer.exe\" %1")]
    [InlineData("\"C:\\Windows\\System32\\cmd.exe\" /c start %1")]
    [InlineData("\"C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe\" Start-Process %1")]
    public void SelfOrShellDispatchers_AreRefused(string template)
    {
        Assert.False(BrowserCommand.TryBuild(template, "https://e.com/", null, out _, out string? err));
        Assert.Contains("ループ", err);
    }

    [Fact]
    public void CurrentExePath_IsRefused_EvenIfRenamed()
    {
        string self = @"C:\Tools\renamed-launcher.exe";
        Assert.False(BrowserCommand.TryBuild($"\"{self}\" %1", "https://e.com/", self, out _, out _));
    }
}

public class CommandLineBuilderTests
{
    [Theory]
    [InlineData("https://e.com/?a=1&b=2")]
    [InlineData("")]
    [InlineData("a b")]
    [InlineData("a \"quoted\" b")]
    [InlineData("trailing\\")]
    [InlineData("trailing space\\")]
    [InlineData("back\\\\\"slash")]
    [InlineData("tab\there")]
    [InlineData("日本語 ü")]
    public void RoundTripsThroughArgvParsing(string arg)
    {
        string line = CommandLineBuilder.Build(@"C:\Program Files\B\b.exe", ["--x", arg]);
        Assert.Equal([@"C:\Program Files\B\b.exe", "--x", arg], BrowserCommand.Tokenize(line));
    }
}

public class ForwardGuardTests
{
    [Fact]
    public void FreshMarker_ForSameUrl_IsReentry()
    {
        var now = DateTimeOffset.UtcNow;
        string m = ForwardGuard.Format("https://e.com/", now);
        Assert.True(ForwardGuard.IsReentry("https://e.com/", m, now.AddSeconds(1)));
    }

    [Fact]
    public void DifferentUrl_OrOldMarker_IsNotReentry()
    {
        var now = DateTimeOffset.UtcNow;
        string m = ForwardGuard.Format("https://e.com/", now);
        Assert.False(ForwardGuard.IsReentry("https://other.com/", m, now));
        Assert.False(ForwardGuard.IsReentry("https://e.com/", m, now.AddMinutes(1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("123:zz")]
    [InlineData(":abc")]
    public void MalformedMarker_IsIgnored(string marker) =>
        Assert.False(ForwardGuard.IsReentry("https://e.com/", marker, DateTimeOffset.UtcNow));
}

public class AppConfigTests
{
    [Fact]
    public void RoundTrip()
    {
        var c = new AppConfig
        {
            BrowserName = "Google Chrome", BrowserProgId = "ChromeHTML",
            BrowserCommand = "\"C:\\x y\\chrome.exe\" --single-argument %1",
            OriginalHttpsProgId = "ChromeHTML", AutoDetectFallback = false, LegacyPrivateServerLinks = true,
        };
        AppConfig r = AppConfig.Parse(c.Serialize());
        Assert.Equal(c.BrowserName, r.BrowserName);
        Assert.Equal(c.BrowserProgId, r.BrowserProgId);
        Assert.Equal(c.BrowserCommand, r.BrowserCommand);
        Assert.Equal(c.OriginalHttpsProgId, r.OriginalHttpsProgId);
        Assert.False(r.AutoDetectFallback);
        Assert.True(r.LegacyPrivateServerLinks);
    }

    [Fact]
    public void Garbage_YieldsDefaults()
    {
        AppConfig r = AppConfig.Parse("###\n=x\nfoo\nAutoDetectFallback=maybe\r\nBrowserName = A=B \n");
        Assert.True(r.AutoDetectFallback);
        Assert.Equal("A=B", r.BrowserName);
    }

    [Fact]
    public void NewlinesInValues_CannotInjectKeys()
    {
        var c = new AppConfig { BrowserName = "x\nLegacyPrivateServerLinks=true" };
        Assert.False(AppConfig.Parse(c.Serialize()).LegacyPrivateServerLinks);
    }

    [Fact]
    public void SaveLoad_File()
    {
        string dir = Path.Combine(Path.GetTempPath(), "lazyblox-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(dir, "config.ini");
            new AppConfig { BrowserName = "テスト" }.Save(path);
            Assert.Equal("テスト", AppConfig.Load(path).BrowserName);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void MissingFile_YieldsDefaults() =>
        Assert.Equal("", AppConfig.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ini")).BrowserCommand);
}
