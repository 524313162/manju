#!/usr/bin/env python3
"""
更新 manju.db 中的 PromptTemplates 表
将 Infrastructure/SeedData/ 下的 JSON 种子文件同步到数据库
"""

import sqlite3
import json
import sys
from pathlib import Path
from datetime import datetime, timezone

def find_db():
    """查找数据库文件"""
    base = Path.cwd()
    candidates = [
        base / "bin" / "manju.db",
        base / "bin" / "Debug" / "net8.0" / "manju.db",
        base / "bin" / "Debug" / "net9.0" / "manju.db",
        base / "bin" / "Release" / "net8.0" / "manju.db",
        base / "bin" / "Release" / "net9.0" / "manju.db",
        base / "src" / "Web" / "ManjuCraft.Web" / "bin" / "Debug" / "net8.0" / "manju.db",
        base / "src" / "Web" / "ManjuCraft.Web" / "bin" / "Debug" / "net9.0" / "manju.db",
    ]
    for p in candidates:
        if p.exists():
            return p
    return None

def main():
    db_path = find_db()
    if not db_path:
        print("❌ 未找到 manju.db。请先运行一次项目以创建数据库，或手动指定路径。")
        sys.exit(1)
    
    print(f"✅ 找到数据库: {db_path}")
    
    seed_dir = Path("src/Infrastructure/SeedData")
    if not seed_dir.exists():
        seed_dir = Path("Infrastructure/SeedData")
    if not seed_dir.exists():
        print(f"❌ 种子目录不存在: {seed_dir}")
        sys.exit(1)
    
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    cursor = conn.cursor()
    
    # 确保表存在
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS PromptTemplates (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            TemplateType TEXT NOT NULL UNIQUE,
            Content TEXT NOT NULL,
            CreatedTime INTEGER NOT NULL,
            UpdatedTime INTEGER NOT NULL
        )
    """)
    conn.commit()
    
    json_files = list(seed_dir.glob("*.json"))
    updated = 0
    inserted = 0
    
    for json_file in json_files:
        with open(json_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        name = data.get('name')
        template_type = data.get('templateType')
        content = data.get('content')
        
        if not all([name, template_type, content]):
            print(f"⚠️  跳过无效文件: {json_file.name}")
            continue
        
        now = int(datetime.now(timezone.utc).timestamp() * 1000)
        
        # 检查是否存在
        cursor.execute("SELECT Id, Content FROM PromptTemplates WHERE TemplateType = ?", (template_type,))
        row = cursor.fetchone()
        
        if row:
            if row['Content'] != content:
                cursor.execute(
                    "UPDATE PromptTemplates SET Name = ?, Content = ?, UpdatedTime = ? WHERE TemplateType = ?",
                    (name, content, now, template_type)
                )
                print(f"🔄 更新: {template_type} ({name})")
                updated += 1
            else:
                print(f"➖ 无变化: {template_type}")
        else:
            cursor.execute(
                "INSERT INTO PromptTemplates (Name, TemplateType, Content, CreatedTime, UpdatedTime) VALUES (?, ?, ?, ?, ?)",
                (name, template_type, content, now, now)
            )
            print(f"➕ 新增: {template_type} ({name})")
            inserted += 1
    
    conn.commit()
    conn.close()
    print(f"\n✅ 完成: 新增 {inserted} 条, 更新 {updated} 条")

if __name__ == "__main__":
    main()