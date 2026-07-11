<#
.SYNOPSIS
更新 manju.db 中的 PromptTemplates 表，将改进后的分镜提取提示词写入数据库

.DESCRIPTION
此脚本连接到 bin/manju.db (或 bin/Debug/net*/manju.db)，
读取 Infrastructure/SeedData/ 下的 JSON 种子文件，
并更新数据库中对应 TemplateType 的记录。

.PARAMETER DbPath
数据库文件路径，默认自动查找 bin 目录下的 manju.db

.PARAMETER SeedDir
种子数据目录，默认 src/Infrastructure/SeedData

.EXAMPLE
.\UpdatePromptTemplates.ps1

.EXAMPLE
.\UpdatePromptTemplates.ps1 -DbPath "E:\Project\AICoding\manju\bin\manju.db"
#>

param(
    [string]$DbPath = "",
    [string]$SeedDir = "src\Infrastructure\SeedData"
)

# 查找数据库文件
if (-not $DbPath) {
    $possiblePaths = @(
        "bin\manju.db",
        "bin\Debug\net8.0\manju.db",
        "bin\Debug\net9.0\manju.db",
        "bin\Release\net8.0\manju.db",
        "bin\Release\net9.0\manju.db",
        "src\Web\ManjuCraft.Web\bin\Debug\net8.0\manju.db",
        "src\Web\ManjuCraft.Web\bin\Debug\net9.0\manju.db"
    )
    
    foreach ($p in $possiblePaths) {
        $fullPath = Join-Path (Get-Location) $p
        if (Test-Path $fullPath) {
            $DbPath = $fullPath
            Write-Host "找到数据库: $DbPath" -ForegroundColor Green
            break
        }
    }
    
    if (-not $DbPath) {
        Write-Error "未找到 manju.db。请先运行一次项目以创建数据库，或使用 -DbPath 指定路径。"
        exit 1
    }
}

$SeedPath = Join-Path (Get-Location) $SeedDir
if (-not (Test-Path $SeedPath)) {
    Write-Error "种子目录不存在: $SeedPath"
    exit 1
}

# 加载 SQLite 库
Add-Type -Path "C:\Program Files\SQLite\sqlite3.dll" -ErrorAction SilentlyContinue
if (-not ([System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "System.Data.SQLite" })) {
    # 尝试从 NuGet 包加载
    $sqliteDll = Get-ChildItem -Recurse -Filter "System.Data.SQLite.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($sqliteDll) {
        Add-Type -Path $sqliteDll.FullName
        Write-Host "已加载 System.Data.SQLite: $($sqliteDll.FullName)" -ForegroundColor Green
    } else {
        Write-Error "未找到 System.Data.SQLite.dll。请安装 NuGet 包: Install-Package System.Data.SQLite.Core"
        exit 1
    }
}

$connStr = "Data Source=$DbPath;Version=3;"
$conn = New-Object System.Data.SQLite.SQLiteConnection($connStr)
$conn.Open()

try {
    $jsonFiles = Get-ChildItem -Path $SeedPath -Filter "*.json"
    $updated = 0
    $inserted = 0
    
    foreach ($file in $jsonFiles) {
        $json = Get-Content $file.FullName -Raw | ConvertFrom-Json
        $name = $json.name
        $templateType = $json.templateType
        $content = $json.content
        
        if (-not $name -or -not $templateType -or -not $content) {
            Write-Warning "跳过无效文件: $($file.Name)"
            continue
        }
        
        # 检查是否存在
        $checkCmd = $conn.CreateCommand()
        $checkCmd.CommandText = "SELECT Id, Content FROM PromptTemplates WHERE TemplateType = @type"
        $checkCmd.Parameters.AddWithValue("@type", $templateType) | Out-Null
        $reader = $checkCmd.ExecuteReader()
        
        if ($reader.Read()) {
            $existingId = $reader.GetInt64(0)
            $existingContent = $reader.GetString(1)
            $reader.Close()
            
            if ($existingContent -ne $content) {
                $updateCmd = $conn.CreateCommand()
                $updateCmd.CommandText = "UPDATE PromptTemplates SET Name = @name, Content = @content, UpdatedTime = @time WHERE TemplateType = @type"
                $updateCmd.Parameters.AddWithValue("@name", $name)
                $updateCmd.Parameters.AddWithValue("@content", $content)
                $updateCmd.Parameters.AddWithValue("@time", [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())
                $updateCmd.Parameters.AddWithValue("@type", $templateType)
                $updateCmd.ExecuteNonQuery() | Out-Null
                Write-Host "✓ 更新: $templateType ($name)" -ForegroundColor Yellow
                $updated++
            } else {
                Write-Host "= 无变化: $templateType" -ForegroundColor Gray
            }
        } else {
            $reader.Close()
            $insertCmd = $conn.CreateCommand()
            $insertCmd.CommandText = "INSERT INTO PromptTemplates (Name, TemplateType, Content, CreatedTime, UpdatedTime) VALUES (@name, @type, @content, @time, @time)"
            $now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
            $insertCmd.Parameters.AddWithValue("@name", $name)
            $insertCmd.Parameters.AddWithValue("@type", $templateType)
            $insertCmd.Parameters.AddWithValue("@content", $content)
            $insertCmd.Parameters.AddWithValue("@time", $now)
            $insertCmd.ExecuteNonQuery() | Out-Null
            Write-Host "✓ 新增: $templateType ($name)" -ForegroundColor Green
            $inserted++
        }
    }
    
    Write-Host "`n完成: 新增 $inserted 条, 更新 $updated 条" -ForegroundColor Cyan
}
finally {
    $conn.Close()
    $conn.Dispose()
}