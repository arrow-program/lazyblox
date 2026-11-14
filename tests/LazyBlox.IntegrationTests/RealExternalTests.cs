namespace LazyBlox.IntegrationTests;

/// <summary>
/// Opt-in tests against real external applications. Run only with explicit consent:
///   $env:LAZYBLOX_REAL_TESTS = "1"
///   $env:LAZYBLOX_REAL_SHARE_URL = "https://www.roblox.com/share?code=...&amp;type=Server"   (a server you may join)
///   dotnet test tests/LazyBlox.IntegrationTests --filter "FullyQualifiedName~RealExternal"
/// </summary>
public class RealExternalTests
{
    [RealExternalFact]
    public void RealRoblox_ShareLink_IsAcceptedByShell()
    {
        string? url = Environment.GetEnvironmentVariable("LAZYBLOX_REAL_SHARE_URL");
        Assert.False(string.IsNullOrEmpty(url), "LAZYBLOX_REAL_SHARE_URL is required");
        RouteDecision d = UrlRouter.Route(url);
        Assert.Equal(RouteKind.Roblox, d.Kind);
        Assert.True(WindowsShell.Instance.IsProtocolRegistered("roblox"), "Roblox is not installed");
        LaunchResult r = WindowsShell.Instance.OpenUri(d.Target); // launches the Roblox client
        Assert.True(r.Success, r.Error);
    }

    [RealExternalFact]
    public void RealBrowser_OpensExampleDotCom()
    {
        using var h = new Harness();
        h.WriteConfig("", autoDetect: true); // use the browser detected on this machine
        var (exit, _, err) = h.Run("https://example.com/?lazyblox-real-test");
        Assert.True(exit == 0, err);
    }
}
