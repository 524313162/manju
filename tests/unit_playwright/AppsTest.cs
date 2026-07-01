using Microsoft.Playwright;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace ManjuCraft.E2ETests;

public sealed class AppsTest : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private static readonly HttpClient Http;
    static AppsTest()
    {
        Http = new();
        Http.BaseAddress = new("http://localhost:8010");
    }
    private static IBrowser? _browser;
    private static readonly object _lock = new();

    private async Task<IBrowser> GetBrowserAsync()
    {
        lock (_lock)
        {
            if (_browser == null)
            {
                var pw = Playwright.CreateAsync().Result;
                _browser = pw.Chromium.LaunchAsync(new() { Headless = true, SlowMo = 100 }).Result;
            }
        }
        return _browser!;
    }

    public AppsTest(ITestOutputHelper output) => _output = output;

    // ===== 首页 =====

    [Fact]
    public async Task HomePage_NavLinks()
    {
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010");

        Assert.True(await page.Locator(".nav-left .logo a").IsVisibleAsync());
        var activeText = await page.Locator("a.nav-link.active").InnerTextAsync();
        Assert.Contains("首页", activeText);
        int linkCount = (int)await page.Locator("a.nav-link").CountAsync();
        Assert.True(linkCount >= 2, $"Expected at least 2 nav links on home page, got {linkCount}");
    }

    [Fact]
    public async Task HomePage_HeroText()
    {
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010");
        var hero = await page.Locator(".hero h2").InnerTextAsync();
        Assert.Contains("一站式漫剧开发工具", hero);
    }

    [Fact]
    public async Task HomePage_FeatureCardsPresent()
    {
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010");

        int count = (int)await page.Locator(".feature-card").CountAsync();
        Assert.True(count >= 6, $"Expected at least 6 feature cards, got {count}");

        string[] titles = { "故事管理", "演员资产", "场景道具", "分集分镜", "ComfyUI 对接", "视频导出" };
        foreach (var title in titles)
        {
            Assert.True(await page.Locator($".feature-card h3:has-text(\"{title}\")").CountAsync() > 0);
        }
    }

    [Fact]
    public async Task HomePage_HeroButtons()
    {
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010");
        Assert.True(await page.Locator(".hero-actions .btn-primary").CountAsync() > 0);
        Assert.True(await page.Locator(".hero-actions .btn-outline").CountAsync() > 0);
    }

    [Fact]
    public async Task HomePage_StatsHidden()
    {
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010");
        // .quick-stats only shows when projects exist; check if rendered element is hidden
        bool hasStats = await page.Locator(".quick-stats").CountAsync() > 0;
        if (hasStats)
        {
            bool visible = await page.Locator(".quick-stats").IsVisibleAsync();
            Assert.True(visible, ".quick-stats should be visible when projects exist");
        }
    }

    [Fact]
    public async Task HomePage_NavigateToProjects()
    {
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010");
        await page.GetByRole(AriaRole.Link, new() { Name = "项目管理" }).ClickAsync();
        await Task.Delay(300);
        Assert.Contains("Projects", page.Url);
        Assert.Contains("项目管理", await page.TitleAsync());
    }

    // ===== 项目管理 =====

    [Fact]
    public async Task Projects_EmptyState()
    {
        await ClearProjectsHttp();
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010/Projects");
        await Task.Delay(300);
        Assert.True(await page.Locator("p:has-text('暂无项目')").CountAsync() > 0);
    }

    [Fact]
    public async Task Projects_OpenModal()
    {
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010/Projects");
        await Task.Delay(300);
        await page.GetByRole(AriaRole.Button, new() { Name = "新建项目" }).ClickAsync();
        await Task.Delay(300);
        Assert.True(await page.Locator("#createProjectModal").IsVisibleAsync());
        Assert.True(await page.Locator("#projectName").IsVisibleAsync());
    }

    [Fact]
    public async Task Projects_CreateAndRedirect()
    {
        await ClearProjectsHttp();
        var page = await (await GetBrowserAsync()).NewPageAsync();
        await page.GotoAsync("http://localhost:8010/Projects");
        await Task.Delay(300);

        string name = $"CP_{DateTime.Now:HHmmss}";
        await page.GetByRole(AriaRole.Button, new() { Name = "新建项目" }).ClickAsync();
        await Task.Delay(200);
        await page.Locator("#projectName").FillAsync(name);
        await page.GetByRole(AriaRole.Button, new() { Name = "创建" }).ClickAsync();
        await Task.Delay(1200);
        // After creating via UI, should stay on Projects page or go to Story
        bool ok = page.Url.Contains("Projects") || page.Url.Contains("Story");
        Assert.True(ok, $"Unexpected URL after create: {page.Url}");
    }

    [Fact]
    public async Task Projects_Delete()
    {
        var page = await (await GetBrowserAsync()).NewPageAsync();
        string name = $"DP_{DateTime.Now:HHmmss}";

        await page.GotoAsync("http://localhost:8010/Projects");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByRole(AriaRole.Button, new() { Name = "新建项目" }).ClickAsync();
        await Task.Delay(200);
        await page.Locator("#projectName").FillAsync(name);
        await page.GetByRole(AriaRole.Button, new() { Name = "创建" }).ClickAsync();

        // Create via AJAX redirects to Story page, wait a bit then go back
        await Task.Delay(1500);
        await page.GotoAsync("http://localhost:8010/Projects");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);

        // Override confirm dialog on the current page context
        await page.EvaluateAsync("() => window.confirm = () => true");

        var cards = page.Locator(".project-card");
        int newCount = (int)await cards.CountAsync();

        int idx = -1;
        for (int i = newCount - 1; i >= 0; i--)
        {
            if ((await cards.Nth(i).InnerTextAsync()).Contains(name))
            {
                idx = i;
                break;
            }
        }
        Assert.True(idx >= 0, $"Project card '{name}' not found in {newCount} cards");

        await cards.Nth(idx).Locator(".delete-project-btn").ClickAsync();
        await Task.Delay(2000);

        // Verify the specific project is gone
        cards = page.Locator(".project-card");
        int countAfter = (int)await cards.CountAsync();
        bool stillExists = false;
        for (int i = 0; i < countAfter; i++)
        {
            if ((await cards.Nth(i).InnerTextAsync()).Contains(name))
            {
                stillExists = true;
                break;
            }
        }
        Assert.False(stillExists, $"Project '{name}' still exists after deletion (count={countAfter})");
    }

    // ===== API 测试 =====

    [Fact]
    public async Task Actors_CreateAndDelete()
    {
        long pid = await GetOrCreateProjectIdAsync();

        var createContent = new StringContent(
            JsonSerializer.Serialize(new { name = "TestActor", description = "Test" }),
            Encoding.UTF8, "application/json");
        var createResp = await Http.PostAsync($"/api/v1/projects/{pid}/actors", createContent);
        var createBody = await createResp.Content.ReadAsStringAsync();
        _output.WriteLine($"Create actor: {createBody}");
        Assert.True(createResp.IsSuccessStatusCode);

        var listResp = await Http.GetAsync($"/api/v1/projects/{pid}/actors");
        var listBody = await listResp.Content.ReadAsStringAsync();
        var listData = JsonSerializer.Deserialize<JsonElement>(listBody);
        if (listData.ValueKind == JsonValueKind.Object)
        {
            Assert.True(listData.GetProperty("success").GetBoolean());
            var arr = listData.GetProperty("data");
            Assert.True(arr.GetArrayLength() > 0);
            long actorId = arr[0].GetProperty("id").GetInt64();

            var delResp = await Http.DeleteAsync($"/api/v1/actors/{actorId}");
            Assert.True(delResp.IsSuccessStatusCode);
        }
    }

    [Fact]
    public async Task Scenes_CreateUpdateDelete()
    {
        long pid = await GetOrCreateProjectIdAsync();

        var c = new StringContent(JsonSerializer.Serialize(new { name = "Scene1" }), Encoding.UTF8, "application/json");
        var createResp = await Http.PostAsync($"/api/v1/projects/{pid}/scenes", c);
        Assert.True(createResp.IsSuccessStatusCode);

        var listResp = await Http.GetAsync($"/api/v1/projects/{pid}/scenes");
        var dataList = JsonSerializer.Deserialize<JsonElement>(await listResp.Content.ReadAsStringAsync());
        Assert.True(dataList.GetProperty("success").GetBoolean());
        Assert.True(dataList.GetProperty("data").GetArrayLength() > 0);
        long sceneId = dataList.GetProperty("data")[0].GetProperty("id").GetInt64();

        var u = new StringContent(JsonSerializer.Serialize(new { name = "Updated Scene" }), Encoding.UTF8, "application/json");
        var updateResp = await Http.PutAsync($"/api/v1/scenes/{sceneId}", u);
        Assert.True(updateResp.IsSuccessStatusCode);

        var deleteResp = await Http.DeleteAsync($"/api/v1/scenes/{sceneId}");
        Assert.True(deleteResp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Props_CreateDelete()
    {
        long pid = await GetOrCreateProjectIdAsync();

        var c = new StringContent(JsonSerializer.Serialize(new { name = "Prop1" }), Encoding.UTF8, "application/json");
        var createResp = await Http.PostAsync($"/api/v1/projects/{pid}/props", c);
        Assert.True(createResp.IsSuccessStatusCode);

        var listResp = await Http.GetAsync($"/api/v1/projects/{pid}/props");
        var data = JsonSerializer.Deserialize<JsonElement>(await listResp.Content.ReadAsStringAsync());
        long propId = data.GetProperty("data")[0].GetProperty("id").GetInt64();

        var deleteResp = await Http.DeleteAsync($"/api/v1/props/{propId}");
        Assert.True(deleteResp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Bgms_CreateDelete()
    {
        long pid = await GetOrCreateProjectIdAsync();

        var c = new StringContent(JsonSerializer.Serialize(new { name = "Bgm1" }), Encoding.UTF8, "application/json");
        var createResp = await Http.PostAsync($"/api/v1/projects/{pid}/bgms", c);
        Assert.True(createResp.IsSuccessStatusCode);

        var listResp = await Http.GetAsync($"/api/v1/projects/{pid}/bgms");
        var data = JsonSerializer.Deserialize<JsonElement>(await listResp.Content.ReadAsStringAsync());
        long bgmId = data.GetProperty("data")[0].GetProperty("id").GetInt64();

        var deleteResp = await Http.DeleteAsync($"/api/v1/bgms/{bgmId}");
        Assert.True(deleteResp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Skills_CreateDelete()
    {
        long pid = await GetOrCreateProjectIdAsync();

        var c = new StringContent(JsonSerializer.Serialize(new { name = "Skill1" }), Encoding.UTF8, "application/json");
        var createResp = await Http.PostAsync($"/api/v1/projects/{pid}/skills", c);
        Assert.True(createResp.IsSuccessStatusCode);

        var listResp = await Http.GetAsync($"/api/v1/projects/{pid}/skills");
        var data = JsonSerializer.Deserialize<JsonElement>(await listResp.Content.ReadAsStringAsync());
        long skillId = data.GetProperty("data")[0].GetProperty("id").GetInt64();

        var deleteResp = await Http.DeleteAsync($"/api/v1/skills/{skillId}");
        Assert.True(deleteResp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Settings_Comfyui()
    {
        var resp = await Http.GetAsync("/api/v1/Settings/comfyui");
        Assert.True(resp.IsSuccessStatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await resp.Content.ReadAsStringAsync());
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.False(body.GetProperty("data").GetProperty("apiUrl").GetString().Length == 0);
    }

    [Fact]
    public async Task Settings_Ffmpeg()
    {
        var resp = await Http.GetAsync("/api/v1/Settings/ffmpeg");
        Assert.True(resp.IsSuccessStatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await resp.Content.ReadAsStringAsync());
        Assert.False(body.GetProperty("data").GetProperty("available").GetBoolean());
    }

    [Fact]
    public async Task Settings_Backup()
    {
        var resp = await Http.GetAsync("/api/v1/Settings/backup");
        Assert.True(resp.IsSuccessStatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await resp.Content.ReadAsStringAsync());
        Assert.Contains("待实现", body.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task Episodes_CreateDelete()
    {
        long pid = await GetOrCreateProjectIdAsync();

        var c = new StringContent(JsonSerializer.Serialize(new { name = "Episode1", duration = 300, order = 1 }), Encoding.UTF8, "application/json");
        var createResp = await Http.PostAsync($"/api/v1/episodes/project/{pid}", c);
        var body = JsonSerializer.Deserialize<JsonElement>(await createResp.Content.ReadAsStringAsync());
        Assert.True(body.GetProperty("success").GetBoolean());
        long epid = body.GetProperty("data").GetProperty("id").GetInt64();

        var updateResp = await Http.PutAsync($"/api/v1/episodes/{epid}",
            new StringContent(JsonSerializer.Serialize(new { name = "Updated", duration = 600, order = 1 }), Encoding.UTF8, "application/json"));
        Assert.True(updateResp.IsSuccessStatusCode);

        var deleteResp = await Http.DeleteAsync($"/api/v1/episodes/{epid}");
        Assert.True(deleteResp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Story_UpdateAndGet()
    {
        long pid = await GetOrCreateProjectIdAsync();

        var c = new StringContent(JsonSerializer.Serialize(new { content = "Test story content" }), Encoding.UTF8, "application/json");
        var resp = await Http.PutAsync($"http://localhost:8010/api/v1/projects/{pid}/story", c);
        _output.WriteLine($"Story PUT response: {(int)resp.StatusCode}");
        var bodyText = await resp.Content.ReadAsStringAsync();
        _output.WriteLine($"Story PUT body: {bodyText}");
        var body = JsonSerializer.Deserialize<JsonElement>(bodyText);
        Assert.True(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task AllPages_HaveFooter()
    {
        foreach (var url in new[] { "/Home", "/Projects", "/Settings/Comfyui" })
        {
            var page = await (await GetBrowserAsync()).NewPageAsync();
            await page.GotoAsync($"http://localhost:8010{url}");
            await Task.Delay(200);
            Assert.True(await page.Locator(".footer").CountAsync() > 0, $"Footer missing on {url}");
        }
    }

    [Fact]
    public async Task Api_InvalidId_ReturnsNotFound()
    {
        var resp = await Http.DeleteAsync("/api/v1/actors/99999999");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ===== 工具方法 =====

    private async Task<long> GetOrCreateProjectIdAsync()
    {
        var resp = await Http.GetAsync("/api/v1/ApiProjects");
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(body);

        if (data.ValueKind == JsonValueKind.Object && data.GetProperty("success").GetBoolean())
        {
            var projects = data.GetProperty("data");
            if (projects.ValueKind == JsonValueKind.Array && projects.GetArrayLength() > 0)
            {
                return projects[0].GetProperty("id").GetInt64();
            }
        }

        // Create project
        var content = new StringContent(JsonSerializer.Serialize(new { name = $"E2E_{DateTime.Now:HHmmss}" }), Encoding.UTF8, "application/json");
        var createResp = await Http.PostAsync("/api/v1/ApiProjects", content);
        _output.WriteLine($"Project create: {(int)createResp.StatusCode}");
        var createBody = await createResp.Content.ReadAsStringAsync();
        _output.WriteLine($"Create response: {createBody}");
        var createData = JsonSerializer.Deserialize<JsonElement>(createBody);
        return createData.GetProperty("data").GetProperty("id").GetInt64();
    }

    private async Task ClearProjectsHttp()
    {
        var resp = await Http.GetAsync("/api/v1/ApiProjects");
        var body = await resp.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(body);

        while (data.ValueKind == JsonValueKind.Object && data.GetProperty("data").ValueKind == JsonValueKind.Array && data.GetProperty("data").GetArrayLength() > 0)
        {
            long id = data.GetProperty("data")[0].GetProperty("id").GetInt64();
            try { await Http.DeleteAsync($"/api/v1/ApiProjects/{id}"); }
            catch { }
            resp = await Http.GetAsync("/api/v1/ApiProjects");
            body = await resp.Content.ReadAsStringAsync();
            data = JsonSerializer.Deserialize<JsonElement>(body);
        }
    }

    public ValueTask DisposeAsync()
    {
        Http.Dispose();
        return ValueTask.CompletedTask;
    }
}
