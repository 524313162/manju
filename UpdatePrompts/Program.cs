using Microsoft.Data.Sqlite;
using System.Text.Json;

var isVerify = args.Contains("--verify");
var isVerifyProviders = args.Contains("--verify-providers");
var isSeedProviders = args.Contains("--seed-providers");
var isFixNvidia = args.Contains("--fix-nvidia");
var dbPath = args.FirstOrDefault(a => !a.StartsWith("--")) ?? @"E:\Project\AICoding\manju\src\Web\ManjuCraft.Web\bin\Debug\net10.0\manju.db";
var seedDir = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : @"E:\Project\AICoding\manju\src\Infrastructure\SeedData";

if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"Database not found: {dbPath}");
    return 1;
}

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

if (isVerifyProviders)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Name, Type, Capability, ApiUrl, Model FROM ApiProviders ORDER BY Name";

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        Console.WriteLine($"{reader.GetString(0)} | Type: {reader.GetString(1)} | Cap: {reader.GetString(2)} | URL: {reader.GetString(3)} | Model: {reader.GetString(4)}");
    }
    return 0;
}

if (isSeedProviders)
{
    return await SeedNvidiaProvidersAsync(conn);
}

if (isFixNvidia)
{
    return await FixNvidiaProvidersAsync(conn);
}

if (isVerify)
{
    var types = new[] { "AssetExtraction", "ShotAssetExtraction", "ShotFrameAssetExtraction", "StoryboardExtraction" };
    
    foreach (var type in types)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Name, TemplateType, Content, UpdatedTime FROM PromptTemplates WHERE TemplateType = @type";
        cmd.Parameters.AddWithValue("@type", type);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var content = reader.GetString(2);
            Console.WriteLine($"=== {reader.GetString(1)} ===");
            Console.WriteLine($"Name: {reader.GetString(0)}");
            Console.WriteLine($"Updated: {DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(3)).ToLocalTime()}");
            Console.WriteLine($"Content length: {content.Length} chars");
            Console.WriteLine();
            Console.WriteLine("--- CONTENT ---");
            Console.WriteLine(content);
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"NOT FOUND: {type}");
        }
    }
    return 0;
}

// Update mode
if (!Directory.Exists(seedDir))
{
    Console.Error.WriteLine($"SeedData directory not found: {seedDir}");
    return 1;
}

var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
int updated = 0, inserted = 0;

var jsonFiles = Directory.GetFiles(seedDir, "*.json")
    .Where(f => Path.GetFileName(f) != "skill-profile.json");

foreach (var file in jsonFiles)
{
    try
    {
        var json = File.ReadAllText(file);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var name = root.GetProperty("name").GetString() ?? "";
        var templateType = root.GetProperty("templateType").GetString() ?? "";
        var content = root.GetProperty("content").GetString() ?? "";

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(templateType) || string.IsNullOrEmpty(content))
        {
            Console.WriteLine($"  ⚠ Skip invalid: {Path.GetFileName(file)}");
            continue;
        }

        long? existingId = null;
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT Id FROM PromptTemplates WHERE TemplateType = @type";
            checkCmd.Parameters.AddWithValue("@type", templateType);
            var result = checkCmd.ExecuteScalar();
            if (result != null) existingId = Convert.ToInt64(result);
        }

        if (existingId.HasValue)
        {
            using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = @"
UPDATE PromptTemplates 
SET Name = @name, Content = @content, UpdatedTime = @updated, IsDefault = 1
WHERE Id = @id";
            updateCmd.Parameters.AddWithValue("@name", name);
            updateCmd.Parameters.AddWithValue("@content", content);
            updateCmd.Parameters.AddWithValue("@updated", now);
            updateCmd.Parameters.AddWithValue("@id", existingId.Value);
            updateCmd.ExecuteNonQuery();
            Console.WriteLine($"  ✓ Updated: {templateType} ({name})");
            updated++;
        }
        else
        {
            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
INSERT INTO PromptTemplates (Name, TemplateType, Content, IsDefault, CreatedTime, UpdatedTime)
VALUES (@name, @type, @content, 1, @created, @updated)";
            insertCmd.Parameters.AddWithValue("@name", name);
            insertCmd.Parameters.AddWithValue("@type", templateType);
            insertCmd.Parameters.AddWithValue("@content", content);
            insertCmd.Parameters.AddWithValue("@created", now);
            insertCmd.Parameters.AddWithValue("@updated", now);
            insertCmd.ExecuteNonQuery();
            Console.WriteLine($"  + Inserted: {templateType} ({name})");
            inserted++;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ✗ Error processing {Path.GetFileName(file)}: {ex.Message}");
    }
}

Console.WriteLine($"\nDone! Updated: {updated}, Inserted: {inserted}");
return 0;

static async Task<int> SeedNvidiaProvidersAsync(SqliteConnection conn)
{
    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
    var nvidiaProviders = new[]
    {
        new { Name = "NVIDIA NIM - Nemotron 3 Ultra",       Capability = 0, Type = 0, ApiUrl = "https://integrate.api.nvidia.com/v1", Model = "nvidia/nemotron-3-ultra" },
        new { Name = "NVIDIA NIM - Nemotron 4 340B",       Capability = 0, Type = 0, ApiUrl = "https://integrate.api.nvidia.com/v1", Model = "nvidia/nemotron-4-340b-instruct" },
        new { Name = "NVIDIA NIM - Llama 3.1 70B",         Capability = 0, Type = 0, ApiUrl = "https://integrate.api.nvidia.com/v1", Model = "meta/llama-3.1-70b-instruct" },
        new { Name = "NVIDIA NIM - Llama 3.1 405B",        Capability = 0, Type = 0, ApiUrl = "https://integrate.api.nvidia.com/v1", Model = "meta/llama-3.1-405b-instruct" },
        new { Name = "NVIDIA NIM - Cosmos 1.0 Diffusion",  Capability = 4, Type = 0, ApiUrl = "https://integrate.api.nvidia.com/v1", Model = "nvidia/cosmos-1.0-diffusion" },
        new { Name = "NVIDIA NIM - Stable Diffusion XL",   Capability = 1, Type = 0, ApiUrl = "https://integrate.api.nvidia.com/v1", Model = "stabilityai/sdxl" },
    };

    int inserted = 0;
    
    foreach (var p in nvidiaProviders)
    {
        // Check if exists
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM ApiProviders WHERE Name = @name AND Model = @model";
        checkCmd.Parameters.AddWithValue("@name", p.Name);
        checkCmd.Parameters.AddWithValue("@model", p.Model);
        var count = Convert.ToInt64(checkCmd.ExecuteScalar());
        
        if (count > 0)
        {
            Console.WriteLine($"  = Exists: {p.Name} ({p.Model})");
            continue;
        }
        
        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = @"
INSERT INTO ApiProviders (Name, Type, Capability, ApiUrl, ApiKey, Model, CreatedTime, UpdatedTime)
VALUES (@name, @type, @capability, @apiUrl, '', @model, @created, @updated)";
        insertCmd.Parameters.AddWithValue("@name", p.Name);
        insertCmd.Parameters.AddWithValue("@type", p.Type);
        insertCmd.Parameters.AddWithValue("@capability", p.Capability);
        insertCmd.Parameters.AddWithValue("@apiUrl", p.ApiUrl);
        insertCmd.Parameters.AddWithValue("@model", p.Model);
        insertCmd.Parameters.AddWithValue("@created", now);
        insertCmd.Parameters.AddWithValue("@updated", now);
        insertCmd.ExecuteNonQuery();
        
        Console.WriteLine($"  + Inserted: {p.Name} ({p.Model})");
        inserted++;
    }
    
    Console.WriteLine($"\nDone! NVIDIA providers inserted: {inserted}");
    return 0;
}

static async Task<int> FixNvidiaProvidersAsync(SqliteConnection conn)
{
    // Correct mappings: Type=LLM(1), Capability: TextToText=1, TextToImage=2, TextToVideo=4
    var fixes = new[]
    {
        new { Name = "NVIDIA NIM - Nemotron 3 Ultra",       Type = 1, Capability = 1 },
        new { Name = "NVIDIA NIM - Nemotron 4 340B",       Type = 1, Capability = 1 },
        new { Name = "NVIDIA NIM - Llama 3.1 70B",         Type = 1, Capability = 1 },
        new { Name = "NVIDIA NIM - Llama 3.1 405B",        Type = 1, Capability = 1 },
        new { Name = "NVIDIA NIM - Cosmos 1.0 Diffusion",  Type = 1, Capability = 4 },
        new { Name = "NVIDIA NIM - Stable Diffusion XL",   Type = 1, Capability = 2 },
    };

    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    int updated = 0;

    foreach (var f in fixes)
    {
        using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = @"
UPDATE ApiProviders 
SET Type = @type, Capability = @capability, UpdatedTime = @updated
WHERE Name = @name";
        updateCmd.Parameters.AddWithValue("@name", f.Name);
        updateCmd.Parameters.AddWithValue("@type", f.Type);
        updateCmd.Parameters.AddWithValue("@capability", f.Capability);
        updateCmd.Parameters.AddWithValue("@updated", now);
        var rows = updateCmd.ExecuteNonQuery();
        
        if (rows > 0)
        {
            Console.WriteLine($"  ✓ Fixed: {f.Name} (Type={f.Type}, Cap={f.Capability})");
            updated++;
        }
        else
        {
            Console.WriteLine($"  ! Not found: {f.Name}");
        }
    }

    Console.WriteLine($"\nDone! NVIDIA providers fixed: {updated}");
    return 0;
}