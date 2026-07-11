<#
.SYNOPSIS
    更新 SQLite 数据库中的 PromptTemplates 表
.DESCRIPTION
    将 SeedData/*.json 中的内容同步到 bin/manju.db 的 PromptTemplates 表
#>

param(
    [string]$DbPath = "E:\Project\AICoding\manju\src\Web\ManjuCraft.Web\bin\Debug\net10.0\manju.db",
    [string]$SeedDataDir = "E:\Project\AICoding\manju\src\Infrastructure\SeedData"
)

Add-Type -AssemblyName System.Data.SQLite

if (-not (Test-Path $DbPath)) {
    Write-Error "数据库文件不存在: $DbPath"
    exit 1
}

if (-not (Test-Path $SeedDataDir)) {
    Write-Error "SeedData 目录不存在: $SeedDataDir"
    exit 1
}

$connStr = "Data Source=$DbPath;Version=3;"
$conn = New-Object System.Data.SQLite.SQLiteConnection($connStr)
$conn.Open()

$now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$updated = 0
$inserted = 0

$jsonFiles = Get-ChildItem $SeedDataDir -Filter "*.json" | Where-Object { $_.Name -ne "skill-profile.json" }

foreach ($file in $jsonFiles) {
    $json = Get-Content $file.FullName -Raw | ConvertFrom-Json
    
    $name = $json.name
    $templateType = $json.templateType
    $content = $json.content
    $isDefault = $true  # 种子数据默认为默认模板
    
    if (-not $name -or -not $templateType -or -not $content) {
        Write-Warning "跳过无效文件: $($file.Name)"
        continue
    }
    
    # 检查是否存在
    $checkCmd = $conn.CreateCommand()
    $checkCmd.CommandText = "SELECT Id FROM PromptTemplates WHERE TemplateType = @type"
    $checkCmd.Parameters.AddWithValue("@type", $templateType) | Out-Null
    $existingId = $checkCmd.ExecuteScalar()
    
    if ($existingId) {
        # 更新
        $updateCmd = $conn.CreateCommand()
        $updateCmd.CommandText = @"
UPDATE PromptTemplates 
SET Name = @name, Content = @content, UpdatedTime = @updated, IsDefault = @isDefault
WHERE Id = @id
"@
        $updateCmd.Parameters.AddWithValue("@name", $name) | Out-Null
        $updateCmd.Parameters.AddWithValue("@content", $content) | Out-Null
        $updateCmd.Parameters.AddWithValue("@updated", $now) | Out-Null
        $updateCmd.Parameters.AddWithValue("@isDefault", $isDefault) | Out-Null
        $updateCmd.Parameters.AddWithValue("@id", $existingId) | Out-Null
        $updateCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  ✓ 更新: $templateType ($name)" -ForegroundColor Green
        $updated++
    } else {
        # 插入
        $insertCmd = $conn.CreateCommand()
        $insertCmd.CommandText = @"
INSERT INTO PromptTemplates (Name, TemplateType, Content, IsDefault, CreatedTime, UpdatedTime)
VALUES (@name, @type, @content, @isDefault, @created, @updated)
"@
        $insertCmd.Parameters.AddWithValue("@name", $name) | Out-Null
        $insertCmd.Parameters.AddWithValue("@type", $templateType) | Out-Null
        $insertCmd.Parameters.AddWithValue("@content", $content) | Out-Null
        $insertCmd.Parameters.AddWithValue("@isDefault", $isDefault) | Out-Null
        $insertCmd.Parameters.AddWithValue("@created", $now) | Out-Null
        $insertCmd.Parameters.AddWithValue("@updated", $now) | Out-Null
        $insertCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  + 新增: $templateType ($name)" -ForegroundColor Cyan
        $inserted++
    }
}

$conn.Close()

Write-Host "`n完成! 更新: $updated, 新增: $inserted" -ForegroundColor Yellow