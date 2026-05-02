# 使用指南 - CQRS + SOLID 异常处理系统

## 快速开始

### 1. 基本异常使用

#### 抛出验证异常
```csharp
public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, string>
{
    public async Task<string> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        // 验证失败
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Title is required");
        }

        // 业务逻辑...
    }
}
```

**Response:**
```json
{
  "success": false,
  "code": 400,
  "message": "标题是必需的",
  "detail": "Title is required",
  "timestamp": 1609459200000
}
```

#### 抛出未找到异常
```csharp
public async Task<Post?> GetPostByIdAsync(string id)
{
    var post = await _postRepository.GetByIdAsync(id);

    if (post == null)
    {
        throw new NotFoundException("Post not found", "Post", id);
    }

    return post;
}
```

**Response:**
```json
{
  "success": false,
  "code": 404,
  "message": "文章未找到",
  "detail": "Post not found",
  "timestamp": 1609459200000
}
```

#### 抛出冲突异常
```csharp
public async Task<string> Handle(CreatePostCommand request, CancellationToken cancellationToken)
{
    var existingPost = await _postRepository.GetByTitleAsync(request.Title);

    if (existingPost != null)
    {
        throw new ConflictException("A post with this title already exists");
    }

    // 创建新文章...
}
```

### 2. 添加自定义异常类型

在 `SocialBlog.Core/Exceptions/ApplicationException.cs` 中添加：

```csharp
/// <summary>
/// 频率限制异常
/// </summary>
public class RateLimitException : ApplicationException
{
    public RateLimitException(string message = "Too many requests")
        : base(message, HttpStatusCode.TooManyRequests, "Error_RateLimit") { }
}
```

在 Application 中使用：

```csharp
public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, string>
{
    private readonly RateLimiter _rateLimiter;

    public async Task<string> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        if (!_rateLimiter.AllowRequest(request.AuthorId))
        {
            throw new RateLimitException("You are creating posts too quickly, please wait a moment");
        }

        // 创建文章...
    }
}
```

**无需修改任何其他代码** - 异常处理系统会自动支持！

### 3. 自定义异常处理程序

创建自定义的状态码映射器：

```csharp
// SocialBlog.Application/Services/CustomExceptionStatusCodeMapper.cs
public class CustomExceptionStatusCodeMapper : IExceptionStatusCodeMapper
{
    public int MapToStatusCode(Exception exception)
    {
        return exception switch
        {
            // 自定义映射规则
            RateLimitException => (int)HttpStatusCode.TooManyRequests,

            // 委托给默认映射器
            _ => new DefaultExceptionStatusCodeMapper().MapToStatusCode(exception)
        };
    }
}
```

在 `Program.cs` 中注册：

```csharp
// 用自定义实现替换默认实现
builder.Services.AddScoped<IExceptionStatusCodeMapper, CustomExceptionStatusCodeMapper>();
```

### 4. 自定义日志记录

扩展 `IExceptionLogger` 接口：

```csharp
// SocialBlog.Application/Services/EnhancedExceptionLogger.cs
public class EnhancedExceptionLogger : IExceptionLogger
{
    private readonly ILogger<EnhancedExceptionLogger> _logger;
    private readonly ISentryClient _sentryClient;

    public EnhancedExceptionLogger(ILogger<EnhancedExceptionLogger> logger, ISentryClient sentryClient)
    {
        _logger = logger;
        _sentryClient = sentryClient;
    }

    public void LogException(Exception exception, string? contextInfo = null)
    {
        // 本地日志
        _logger.LogError(exception, "Exception: {ContextInfo}", contextInfo);

        // 发送到 Sentry
        if (exception is not ValidationException)
        {
            _sentryClient.CaptureException(exception);
        }
    }
}
```

注册到 DI：

```csharp
builder.Services.AddScoped<IExceptionLogger, EnhancedExceptionLogger>();
```

## 最佳实践

### ✅ DO: 使用特定的异常类型

```csharp
// ✅ 好 - 清晰表达异常意图
throw new ValidationException("Email format is invalid");
throw new NotFoundException("User not found", "User", userId);
throw new ConflictException("Email already exists");
```

### ❌ DON'T: 使用通用异常

```csharp
// ❌ 不好 - 不清晰
throw new Exception("Something went wrong");
throw new ApplicationException("Error occurred");
```

### ✅ DO: 包含有用的信息

```csharp
// ✅ 好 - 包含所有必要信息
throw new NotFoundException(
    "Post with ID {PostId} not found",
    "Post",
    postId
);
```

### ❌ DON'T: 隐藏异常

```csharp
// ❌ 不好 - 隐藏错误
try
{
    // 某些操作
}
catch
{
    // 静默失败
}
```

### ✅ DO: 在适当的层级处理

```csharp
// ✅ 好 - 在 Command Handler 中验证
public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, string>
{
    public async Task<string> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Title is required");
        }
        // ...
    }
}
```

## 异常类型参考

| 异常类型 | HTTP 状态码 | 何时使用 |
|---------|-----------|--------|
| `ValidationException` | 400 | 用户输入验证失败 |
| `NotFoundException` | 404 | 请求的资源不存在 |
| `UnauthorizedException` | 401 | 用户未身份验证 |
| `ForbiddenException` | 403 | 用户无权访问资源 |
| `ConflictException` | 409 | 资源冲突（如重复条目） |
| `InternalServerException` | 500 | 服务器内部错误 |

## 本地化消息

系统支持多语言错误消息。添加资源文件：

### 中文 (zh-CN)
```
SharedResources.zh-CN.resx

Error_Validation=验证失败
Error_NotFound=资源未找到
Error_Unauthorized=未授权
Error_Forbidden=禁止访问
Error_Conflict=资源冲突
Error_Internal=内部服务器错误
Error_RateLimit=请求频率过高
```

### English (en-US)
```
SharedResources.en-US.resx

Error_Validation=Validation failed
Error_NotFound=Resource not found
Error_Unauthorized=Unauthorized
Error_Forbidden=Forbidden
Error_Conflict=Resource conflict
Error_Internal=Internal server error
Error_RateLimit=Too many requests
```

系统会根据请求的 `Accept-Language` 头自动选择语言。

## 测试异常处理

### 单元测试示例

```csharp
[TestClass]
public class ExceptionHandlingTests
{
    private IExceptionHandler _exceptionHandler;

    [TestInitialize]
    public void Setup()
    {
        var logger = new Mock<IExceptionLogger>();
        var statusCodeMapper = new DefaultExceptionStatusCodeMapper();
        var messageLocalizer = new Mock<IExceptionMessageLocalizer>();

        _exceptionHandler = new ExceptionHandler(
            logger.Object,
            statusCodeMapper,
            messageLocalizer.Object
        );
    }

    [TestMethod]
    public void Handle_ValidationException_Returns400()
    {
        // Arrange
        var exception = new ValidationException("Test validation failed");

        // Act
        var response = _exceptionHandler.Handle(exception);

        // Assert
        Assert.AreEqual(400, response.Code);
        Assert.IsFalse(response.Success);
    }

    [TestMethod]
    public void Handle_NotFoundException_Returns404()
    {
        // Arrange
        var exception = new NotFoundException("Post not found", "Post", "123");

        // Act
        var response = _exceptionHandler.Handle(exception);

        // Assert
        Assert.AreEqual(404, response.Code);
        Assert.IsFalse(response.Success);
    }
}
```

## 常见问题 (FAQ)

### Q: 如何添加新的异常类型？
A: 在 `SocialBlog.Core/Exceptions/ApplicationException.cs` 中继承 `ApplicationException`。无需修改其他代码。

### Q: 如何改变异常映射到的状态码？
A: 创建一个新的 `IExceptionStatusCodeMapper` 实现并在 `Program.cs` 中注册。

### Q: 如何添加本地化支持？
A: 创建资源文件（`.resx`）并在 `LocalizationKey` 中引用。

### Q: 异常是否包含堆栈跟踪？
A: 是的，异常的原始消息（包括堆栈跟踪）会被记录到日志中，但不会返回给客户端（出于安全考虑）。

### Q: 如何区分业务异常和系统异常？
A: 业务异常继承自 `ApplicationException`，系统异常是 `System.Exception` 的其他子类。`DefaultExceptionStatusCodeMapper` 会自动处理两者。

## 相关资源

- 📄 [EXCEPTION_HANDLING_ARCHITECTURE.md](EXCEPTION_HANDLING_ARCHITECTURE.md) - 详细架构文档
- 📄 [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) - 项目结构说明
- 🔗 [SOLID 原则](https://en.wikipedia.org/wiki/SOLID)
- 🔗 [CQRS 模式](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

---

有任何问题？请参考架构文档或创建一个新的异常类型进行测试！
