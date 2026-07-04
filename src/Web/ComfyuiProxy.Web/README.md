# ComfyUI 代理服务 (ComfyuiProxy.Web)

## 项目概述

ComfyUI 代理服务是一个专门用于代理和转发ComfyUI请求的Web API服务。它提供了统一的接口来访问ComfyUI的各种功能，包括工作流执行、状态查询、文件上传等。


## 功能特性

### 核心功能
- ✅ **工作流执行代理** - 转发工作流执行请求到ComfyUI服务器
- ✅ **状态查询代理** - 查询工作流执行状态
- ✅ **文件上传代理** - 上传文件到ComfyUI
- ✅ **系统信息查询** - 获取ComfyUI系统信息
- ✅ **健康检查** - 监控ComfyUI服务状态

### 技术特点
- **轻量级设计** - 仅处理请求转发，不包含业务逻辑
- **高性能** - 使用HttpClient进行高效的HTTP请求转发
- **健康检查** - 集成ASP.NET Core健康检查机制
- **配置化** - 支持通过配置文件配置ComfyUI服务器地址

## 快速开始

### 1. 环境要求

- .NET 6.0 或更高版本
- ComfyUI 服务器（默认运行在 http://localhost:8188）
- 开放的网络端口（默认8188）

### 2. 配置

#### 配置文件：`appsettings.json`

```json
{
  "ComfyUI": {
    "Url": "http://localhost:8188"
  }
}
```

#### 配置说明
- `ComfyUI:Url` - ComfyUI服务器地址，默认 `http://localhost:8188`

### 3. 启动服务

```bash
# 启动代理服务
cd src/Web/ComfyuiProxy.Web
dotnet run

# 服务将运行在 http://localhost:5000
```

### 4. 健康检查

访问健康检查端点：

```bash
curl http://localhost:5000/health
```

预期响应：
```json
{
  "status": "Healthy",
  "results": [
    {
      "name": "comfyui_health_check",
      "status": "Healthy",
      "description": "ComfyUI 服务可用"
    }
  ]
}
```

## API 文档

### 1. 工作流执行

**请求：**
```http
POST /api/Comfyui/execute?promptId={promptId}
Content-Type: application/json

{
  "workflowJson": "{工作流JSON数据}"
}
```

**响应：**
- 200 OK - 工作流执行成功
- 500 Internal Server Error - 执行失败

### 2. 查询工作流状态

**请求：**
```http
GET /api/Comfyui/status/{promptId}
```

**响应：**
- 200 OK - 状态查询成功
- 404 Not Found - 工作流不存在
- 500 Internal Server Error - 查询失败

### 3. 获取系统信息

**请求：**
```http
GET /api/Comfyui/system-info
```

**响应：**
- 200 OK - 系统信息查询成功
- 500 Internal Server Error - 查询失败

### 4. 文件上传

**请求：**
```http
POST /api/Comfyui/upload?subfolder={subfolder}&overwrite={overwrite}
Content-Type: multipart/form-data

[文件内容]
```

**参数：**
- `subfolder` - 可选，上传到的子文件夹
- `overwrite` - 可选，是否覆盖已存在文件，默认false

**响应：**
- 200 OK - 文件上传成功
- 400 Bad Request - 未上传文件
- 500 Internal Server Error - 上传失败

## 配置说明

### 环境变量

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `ComfyUI__Url` | `http://localhost:8188` | ComfyUI服务器地址 |

### 配置文件优先级

1. `appsettings.Development.json`（开发环境）
2. `appsettings.json`（生产环境）
3. 环境变量
4. 代码默认值

## 故障排除

### 1. 健康检查失败

**问题：** 健康检查返回Unhealthy状态

**解决方案：**

1. **验证ComfyUI服务状态**
   ```bash
   curl -v http://localhost:8188
   ```
   - 如果返回404或超时，说明ComfyUI服务未运行
   - 如果返回数据，说明ComfyUI服务正常

2. **检查配置**
   ```bash
   # 查看当前配置
   cat src/Web/ComfyuiProxy.Web/appsettings.json
   ```
   - 确认 `ComfyUI:Url` 配置正确

3. **查看日志**
   ```bash
   # 查看应用程序日志
   dotnet run
   ```
   - 健康检查会输出详细的错误日志

### 2. 连接超时

**问题：** 请求ComfyUI时出现超时错误

**解决方案：**

1. **增加超时时间**
   - 编辑 `Program.cs`
   - 修改HttpClient超时配置

2. **检查网络连接**
   ```bash
   ping localhost
   telnet localhost 8188
   ```

### 3. 端点不存在

**问题：** 访问的端点返回404

**解决方案：**

1. **检查ComfyUI版本**
   - 不同版本的ComfyUI可能有不同的API端点
   - 查看ComfyUI官方文档

2. **使用正确的端点**
   - `/prompt` - 工作流执行
   - `/history/{id}` - 工作流状态查询
   - `/system_info` - 系统信息（部分版本不支持）
   - `/upload` - 文件上传

## 开发指南

### 项目结构

```
ComfyuiProxy.Web/
├── Controllers/          # API控制器
│   └── ComfyuiController.cs
├── Services/            # 服务类
│   └── ComfyuiProxyService.cs
├── Infrastructure/      # 基础设施
│   └── ComfyuiProxyHealthCheck.cs
├── appsettings.json      # 配置文件
├── appsettings.Development.json
└── Program.cs           # 主程序
```

### 添加新API端点

1. **创建控制器**
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class NewController : ControllerBase
   {
       private readonly ComfyuiProxyService _service;
       
       public NewController(ComfyuiProxyService service)
       {
           _service = service;
       }
   }
   ```

2. **添加服务方法**
   ```csharp
   public class ComfyuiProxyService
   {
       public async Task<HttpResponseMessage> NewMethodAsync()
       {
           // 实现代理逻辑
       }
   }
   ```

3. **添加路由**
   ```csharp
   [HttpGet("new-endpoint")]
   public async Task<IActionResult> NewEndpoint()
   {
       // 处理请求
   }
   ```

## 部署

### Docker部署

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Web/ComfyuiProxy.Web/ComfyuiProxy.Web.csproj", "src/Web/ComfyuiProxy.Web/"]
RUN dotnet restore "src/Web/ComfyuiProxy.Web/ComfyuiProxy.Web.csproj"
COPY . .
WORKDIR "/src/src/Web/ComfyuiProxy.Web"
RUN dotnet build "ComfyuiProxy.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ComfyuiProxy.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ComfyuiProxy.Web.dll"]
```

### 环境变量配置

```bash
export ComfyUI__Url="http://comfyui-server:8188"
docker run -p 8080:80 -e ComfyUI__Url="http://comfyui-server:8188" comfyui-proxy
```

## 监控和日志

### 健康检查端点

```bash
GET /health
```

### 监控指标

- **响应时间** - 请求处理时间
- **错误率** - 失败请求的比例
- **吞吐量** - 每秒处理的请求数

### 日志级别

- **Debug** - 开发环境，详细日志
- **Information** - 生产环境，正常操作日志
- **Warning** - 警告信息
- **Error** - 错误信息

## 常见问题

### Q: 如何配置ComfyUI服务器地址？

A: 编辑 `appsettings.json` 文件：

```json
{
  "ComfyUI": {
    "Url": "http://your-comfyui-server:8188"
  }
}
```

### Q: 健康检查为什么失败？

A: 检查以下几点：
1. ComfyUI服务是否正在运行
2. 配置的ComfyUI地址是否正确
3. 网络连接是否正常
4. ComfyUI版本是否兼容

### Q: 如何增加超时时间？

A: 编辑 `Program.cs` 文件，修改HttpClient配置：

```csharp
builder.Services.AddHttpClient<ComfyuiProxyService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(10); // 增加超时时间
})
```

## 贡献指南

欢迎贡献代码！请遵循以下准则：

1. **代码风格** - 遵循C#编码规范
2. **测试覆盖** - 为新功能添加单元测试
3. **文档更新** - 更新相关文档
4. **提交信息** - 使用有意义的提交信息

## 联系方式

如有问题或建议，请联系开发团队。

---

**版本：** v1.0.0  
**最后更新：** 2026-07-04  
**维护者：** AI开发团队