## CQRS 和 SOLID 原则的异常处理架构

### 📋 架构概述

本项目的异常处理系统遵循 **CQRS**（Command Query Responsibility Segregation）和 **SOLID** 原则设计。

---

### 🏗️ 分层结构

```
┌─────────────────────────────────────────────────────────────┐
│                     SocialBlog.Api (表现层)                  │
├─────────────────────────────────────────────────────────────┤
│  ExceptionHandlingMiddleware                                 │
│  ├─ 捕获异常                                                 │
│  └─ 调用 IExceptionHandler 处理异常                          │
└──────────────────────────┬──────────────────────────────────┘
                           │ 依赖
                           ↓
┌─────────────────────────────────────────────────────────────┐
│               SocialBlog.Application (应用层)                │
├─────────────────────────────────────────────────────────────┤
│  IExceptionHandler (主协调器)                               │
│  ├── IExceptionLogger                                        │
│  │   └── ExceptionLogger (日志记录)                         │
│  ├── IExceptionStatusCodeMapper                              │
│  │   └── DefaultExceptionStatusCodeMapper (状态码映射)      │
│  ├── IExceptionMessageLocalizer                              │
│  │   └── ExceptionMessageLocalizer (本地化消息)            │
│  └── ExceptionResponse (响应模型)                            │
└──────────────────────────┬──────────────────────────────────┘
                           │ 依赖
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                 SocialBlog.Core (核心层)                     │
├─────────────────────────────────────────────────────────────┤
│  ApplicationException (异常基类)                             │
│  ├── ValidationException                                     │
│  ├── NotFoundException                                       │
│  ├── UnauthorizedException                                   │
│  ├── ForbiddenException                                      │
│  ├── ConflictException                                       │
│  └── InternalServerException                                 │
└─────────────────────────────────────────────────────────────┘
```

---

### 🎯 SOLID 原则应用

#### 1️⃣ **单一职责原则 (Single Responsibility Principle)**

每个类仅负责一项职责：

| 类 | 职责 |
|---|---|
| **ExceptionHandlingMiddleware** | 捕获异常并调用处理器 |
| **IExceptionLogger / ExceptionLogger** | 仅负责异常日志记录 |
| **IExceptionStatusCodeMapper** | 仅负责映射异常到 HTTP 状态码 |
| **IExceptionMessageLocalizer** | 仅负责获取本地化错误消息 |
| **IExceptionHandler** | 协调所有服务处理异常 |

#### 2️⃣ **开闭原则 (Open/Closed Principle)**

代码对扩展开放，对修改关闭：

```csharp
// 新增异常类型 - 无需修改现有代码
public class RateLimitException : ApplicationException
{
    public RateLimitException(string message)
        : base(message, HttpStatusCode.TooManyRequests, "Error_RateLimit") { }
}
```

#### 3️⃣ **里氏替换原则 (Liskov Substitution Principle)**

所有异常都继承自 `ApplicationException`，可互换使用：

```csharp
// 可以使用任何 ApplicationException 子类
public void HandleException(ApplicationException ex)
{
    var statusCode = _statusCodeMapper.MapToStatusCode(ex);
    // ...
}
```

#### 4️⃣ **接口隔离原则 (Interface Segregation Principle)**

使用最小化接口：

```csharp
// 不强制实现不需要的方法
public interface IExceptionLogger
{
    void LogException(Exception exception, string? contextInfo = null);
}

public interface IExceptionStatusCodeMapper
{
    int MapToStatusCode(Exception exception);
}
```

#### 5️⃣ **依赖倒置原则 (Dependency Inversion Principle)**

依赖抽象而非具体实现：

```csharp
public ExceptionHandler(
    IExceptionLogger logger,                      // 依赖抽象
    IExceptionStatusCodeMapper statusCodeMapper,  // 依赖抽象
    IExceptionMessageLocalizer messageLocalizer)  // 依赖抽象
{
    // ...
}
```

---

### 📊 CQRS 原则应用

CQRS 将读操作（Query）和写操作（Command）分离：

#### Command 层（修改数据）

```csharp
// SocialBlog.Application/Commands/
public record CreatePostCommand(...) : IRequest<string>;
public record UpdatePostCommand(...) : IRequest<bool>;
public record DeletePostCommand(...) : IRequest<bool>;
public record PublishPostCommand(...) : IRequest<bool>;
```

#### Query 层（读取数据）

```csharp
// SocialBlog.Application/Queries/
public record GetPostByIdQuery(...) : IRequest<Post?>;
public record GetPublishedPostsQuery(...) : IRequest<List<Post>>;
public record GetPostsByAuthorQuery(...) : IRequest<List<Post>>;
public record SearchPostsQuery(...) : IRequest<List<Post>>;
```

#### 异常处理中遵循 CQRS

```csharp
// IExceptionHandler.Handle - 这是一个 Query（读操作）
// 它读取异常信息并返回响应，不修改任何数据
public interface IExceptionHandler
{
    ExceptionResponse Handle(Exception exception, string? contextInfo = null);
}
```

---

### 🔄 请求处理流程

```
┌─────────────────────────────────────────────────────────────┐
│ 1. HTTP Request 到达                                         │
└────────────────────────┬────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. ExceptionHandlingMiddleware.InvokeAsync()                │
│    - 调用 _next(context)                                    │
└────────────────────────┬────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Controller Action 执行                                    │
│    - 成功 → 响应                                            │
│    - 异常 → 捕获                                            │
└────────────────────────┬────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. HandleExceptionAsync() 被调用                            │
│    - 调用 _exceptionHandler.Handle(exception)               │
└────────────────────────┬────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. ExceptionHandler.Handle()                                │
│    ├─ _logger.LogException(exception)                      │
│    │  └─ 记录异常到日志                                    │
│    ├─ _statusCodeMapper.MapToStatusCode(exception)         │
│    │  └─ 获取 HTTP 状态码                                  │
│    ├─ _messageLocalizer.GetLocalizedMessage(exception)     │
│    │  └─ 获取本地化错误消息                                │
│    └─ 返回 ExceptionResponse                               │
└────────────────────────┬────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 6. 返回 JSON 响应给客户端                                   │
└─────────────────────────────────────────────────────────────┘
```

---

### 💻 使用示例

#### 在 Application 中使用自定义异常

```csharp
public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, string>
{
    public async Task<string> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            // 抛出验证异常 - 自动映射到 400 状态码
            throw new ValidationException("Title is required");
        }

        var post = new Post
        {
            Title = request.Title,
            Content = request.Content,
            AuthorId = request.AuthorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = "Draft"
        };

        await _postRepository.AddAsync(post, cancellationToken);
        return post.Id;
    }
}
```

#### 异常响应示例

```json
{
  "success": false,
  "code": 400,
  "message": "标题是必需的",
  "detail": "Title is required",
  "timestamp": 1609459200000
}
```

---

### 🧪 扩展示例

#### 添加新的异常类型

```csharp
// Core/Exceptions/ApplicationException.cs
public class RateLimitException : ApplicationException
{
    public RateLimitException(string message = "Too many requests")
        : base(message, HttpStatusCode.TooManyRequests, "Error_RateLimit") { }
}
```

#### 无需修改任何其他代码 - 自动支持！

```csharp
// Application/Services/ExceptionStatusCodeMapper.cs 中的 switch 表达式
// 会自动处理新的异常类型
return exception switch
{
    Core.Exceptions.ApplicationException appEx => (int)appEx.StatusCode,  // ✅ 自动支持
    // ...
};
```

---

### 📋 配置 (Program.cs)

```csharp
// 遵循依赖倒置原则 - 注册抽象而非具体实现
builder.Services.AddScoped<IExceptionLogger, ExceptionLogger>();
builder.Services.AddScoped<IExceptionStatusCodeMapper, DefaultExceptionStatusCodeMapper>();
builder.Services.AddScoped<IExceptionMessageLocalizer>(sp =>
{
    var localizerFactory = sp.GetRequiredService<IStringLocalizerFactory>();
    var localizer = localizerFactory.Create("SharedResources", ...);
    return new ExceptionMessageLocalizer(localizer);
});
builder.Services.AddScoped<IExceptionHandler, ExceptionHandler>();
```

---

### ✨ 主要优势

| 优势 | 说明 |
|------|------|
| **易于测试** | 所有依赖注入，可以轻松进行单元测试 |
| **易于维护** | 每个类职责单一，代码清晰 |
| **易于扩展** | 添加新异常类型无需修改现有代码 |
| **易于定制** | 可以轻松替换任何实现（如日志、本地化） |
| **多语言支持** | 内置本地化支持 |
| **SOLID 兼容** | 完全遵循所有 SOLID 原则 |
| **CQRS 兼容** | 异常处理是独立的查询操作 |

---

### 🔗 相关文件

- **Core 层**: `SocialBlog.Core/Exceptions/ApplicationException.cs`
- **Application 层**: 
  - `SocialBlog.Application/Services/ExceptionLogger.cs`
  - `SocialBlog.Application/Services/ExceptionStatusCodeMapper.cs`
  - `SocialBlog.Application/Services/ExceptionMessageLocalizer.cs`
  - `SocialBlog.Application/Services/ExceptionHandler.cs`
  - `SocialBlog.Application/Responses/ExceptionResponse.cs`
- **API 层**: `SocialBlog.Api/Middlewares/ExceptionHandlingMiddleware.cs`
