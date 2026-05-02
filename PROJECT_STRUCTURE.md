# 项目结构 - CQRS + SOLID 原则

## 目录树

```
SocialBlog.Backend/
│
├── SocialBlog.Core/                      # 核心层 - 业务模型和规则
│   ├── Entities/
│   │   └── Post.cs                       # Post 实体
│   ├── Interfaces/
│   │   └── IPostRepository.cs            # 仓储接口
│   └── Exceptions/                       # ✨ 异常定义
│       └── ApplicationException.cs       # 自定义异常层次结构
│
├── SocialBlog.Application/               # 应用层 - 业务逻辑和协调
│   ├── Commands/
│   │   ├── CreatePostCommand.cs          # ✨ Record + IRequest
│   │   ├── UpdatePostCommand.cs          # ✨ Record + IRequest
│   │   ├── DeletePostCommand.cs          # ✨ Record + IRequest
│   │   └── PublishPostCommand.cs         # ✨ Record + IRequest
│   ├── Queries/
│   │   ├── GetPostByIdQuery.cs           # ✨ Record + IRequest
│   │   ├── GetPublishedPostsQuery.cs     # ✨ Record + IRequest
│   │   ├── GetPostsByAuthorQuery.cs      # ✨ Record + IRequest
│   │   └── SearchPostsQuery.cs           # ✨ Record + IRequest
│   ├── Requests/
│   │   └── PostRequests.cs               # 请求包装类
│   ├── Responses/
│   │   ├── PostResponses.cs              # Post 相关响应
│   │   └── ExceptionResponse.cs          # ✨ 异常响应
│   └── Services/                         # ✨ 异常处理服务
│       ├── ExceptionLogger.cs            # 日志记录服务
│       ├── ExceptionStatusCodeMapper.cs  # 状态码映射服务
│       ├── ExceptionMessageLocalizer.cs  # 本地化消息服务
│       └── ExceptionHandler.cs           # 异常处理协调器
│
├── SocialBlog.Infrastructure/            # 基础设施层 - 外部依赖
│   ├── Data/
│   │   └── MongoDbContext.cs             # MongoDB 上下文
│   └── Repositories/
│       └── PostRepository.cs             # Post 仓储实现
│
├── SocialBlog.Api/                       # 表示层 - HTTP API
│   ├── Controllers/
│   │   └── PostsController.cs            # ✨ 使用 AutoMapper 和具体类型
│   ├── Middlewares/
│   │   ├── ExceptionHandlingMiddleware.cs # ✨ 异常处理中间件
│   │   ├── ResponseWrappingMiddleware.cs  # 响应包装中间件
│   │   └── MiddlewareExtensions.cs        # 中间件扩展
│   ├── Models/
│   │   └── ApiResponse.cs                # 统一响应模型
│   ├── Dtos/
│   │   └── PostDtos.cs                   # Post DTO
│   ├── Mappings/
│   │   └── PostMappingProfile.cs         # ✨ AutoMapper 配置
│   ├── Program.cs                        # ✨ DI 注册和配置
│   └── appsettings.json
│
└── EXCEPTION_HANDLING_ARCHITECTURE.md    # ✨ 架构文档

```

## 设计原则应用

### ✨ CQRS（命令查询职责分离）

**Commands** - 修改数据的操作：
- `CreatePostCommand` - 创建文章
- `UpdatePostCommand` - 更新文章
- `DeletePostCommand` - 删除文章
- `PublishPostCommand` - 发布文章

**Queries** - 读取数据的操作：
- `GetPostByIdQuery` - 获取单个文章
- `GetPublishedPostsQuery` - 获取已发布文章列表
- `GetPostsByAuthorQuery` - 获取作者的文章
- `SearchPostsQuery` - 搜索文章

### ✨ SOLID 原则

#### 1. 单一职责原则 (SRP)
```
ExceptionHandlingMiddleware  → 捕获异常
ExceptionLogger              → 记录日志
ExceptionStatusCodeMapper    → 映射状态码
ExceptionMessageLocalizer    → 获取本地化消息
ExceptionHandler             → 协调所有服务
```

#### 2. 开闭原则 (OCP)
- 新增异常类型只需继承 `ApplicationException`
- 无需修改现有的 `ExceptionHandler` 代码
- `switch` 表达式自动支持新异常类型

#### 3. 里氏替换原则 (LSP)
- 所有自定义异常都继承自 `ApplicationException`
- 可以在任何期望 `ApplicationException` 的地方使用

#### 4. 接口隔离原则 (ISP)
```csharp
public interface IExceptionLogger { void LogException(...); }
public interface IExceptionStatusCodeMapper { int MapToStatusCode(...); }
public interface IExceptionMessageLocalizer { string GetLocalizedMessage(...); }
public interface IExceptionHandler { ExceptionResponse Handle(...); }
```

#### 5. 依赖倒置原则 (DIP)
```csharp
public ExceptionHandler(
    IExceptionLogger logger,                      // 依赖抽象
    IExceptionStatusCodeMapper statusCodeMapper,  // 依赖抽象
    IExceptionMessageLocalizer messageLocalizer)  // 依赖抽象
{
}
```

### ✨ 其他设计模式

| 模式 | 应用 |
|------|------|
| **Strategy Pattern** | IExceptionStatusCodeMapper, IExceptionMessageLocalizer |
| **Chain of Responsibility** | 中间件管道 |
| **Dependency Injection** | 贯穿整个应用 |
| **Factory Pattern** | 通过 DI 容器创建服务 |
| **Record Pattern** | Commands/Queries 的参数包装 |
| **Mapping Pattern** | AutoMapper 自动映射 |

## 关键特性

| 特性 | 说明 |
|------|------|
| **多语言支持** | 所有错误消息支持 zh-CN 和 en-US |
| **统一响应格式** | 所有 API 返回一致的响应结构 |
| **自动异常处理** | 全局中间件捕获所有异常 |
| **强类型 DTO** | 使用 Records 和具体类型 |
| **自动映射** | 使用 AutoMapper 进行 DTO 转换 |
| **灵活配置** | 易于添加新的异常类型或修改行为 |

## 数据流

### 成功请求流

```
Request
   ↓
ExceptionHandlingMiddleware (捕获异常)
   ↓
ResponseWrappingMiddleware (包装响应)
   ↓
Controller Action
   ↓
IMediator.Send(Query/Command)
   ↓
Handler (MediatR)
   ↓
Repository
   ↓
Response (ApiResponse<T>)
   ↓
ResponseWrappingMiddleware 包装
   ↓
HTTP Response
```

### 异常请求流

```
Request
   ↓
ExceptionHandlingMiddleware (捕获异常)
   ↓
IExceptionHandler.Handle()
   ├─ IExceptionLogger.LogException()
   ├─ IExceptionStatusCodeMapper.MapToStatusCode()
   ├─ IExceptionMessageLocalizer.GetLocalizedMessage()
   └─ ExceptionResponse
   ↓
HTTP Response (JSON)
```

## 编译和运行

```bash
# 编译
dotnet build

# 运行
dotnet run --project SocialBlog.Api/SocialBlog.Api.csproj
```

## 相关技术栈

- **.NET 10** - 最新版本框架
- **MediatR** - CQRS 实现
- **AutoMapper** - DTO 映射
- **MongoDB** - 数据存储
- **Scalar** - API 文档

---

所有代码遵循 **CQRS** 和 **SOLID** 原则，确保高可维护性和可扩展性。✨
