## MongoDB Collections

### posts
- _id (ObjectId as string)
- authorId (ObjectId as string)
- title (string)
- content (string)
- coverImageUrl (string | null)  
  - 可以是外部 URL，或本项目上传接口返回的 URL（例如 /api/Posts/media/{fileId}?name=xxx.mp4）
- tags (string[])
- status (string) Draft | Published
- likeCount (int)
- commentCount (int)
- createdAt (date)
- updatedAt (date)
- publishedAt (date | null)
- isDeleted (bool)
- deletedAt (date | null)

### comments
- _id (ObjectId as string)
- postId (ObjectId as string)
- authorId (ObjectId as string)
- content (string)  
  - 支持在文本中粘贴链接；前端会将纯链接的图片/视频渲染为媒体，其它链接自动变为可点击链接
- parentCommentId (ObjectId as string | null)  
  - 用于实现回复树：null 表示顶层评论；否则表示回复哪一条评论
- createdAt (date)
- updatedAt (date)

### likes
- _id (ObjectId as string)
- postId (ObjectId as string)
- userId (ObjectId as string)
- createdAt (date)

### users
- _id (ObjectId as string)
- username (string)
- usernameNormalized (string)
- email (string)
- emailNormalized (string)
- passwordHash (string)
- displayName (string | null)
- bio (string | null)
- avatarUrl (string | null)
- coverImageUrl (string | null)
- createdAt (date)
- updatedAt (date)

### follows
- _id (ObjectId as string)
- followerId (ObjectId as string)
- followingId (ObjectId as string)
- createdAt (date)

### refresh_tokens
- _id (ObjectId as string)
- userId (ObjectId as string)
- tokenHash (string)
- createdAt (date)
- expiresAt (date)
- revokedAt (date | null)
- replacedByTokenId (ObjectId as string | null)
- createdByIp (string | null)
- revokedByIp (string | null)

### token_blacklist
- _id (ObjectId as string)
- jti (string)
- expiresAt (date)
- createdAt (date)

## GridFS (Media Storage)

本项目使用 MongoDB GridFS 存储大文件（图片/视频等），避免单文档 16MB 限制。

### media.files
- _id (ObjectId)
- length (long)
- chunkSize (int)
- uploadDate (date)
- filename (string)
- metadata
  - contentType (string) 例如 image/png, video/mp4
  - ext (string) 例如 .png, .mp4
  - originalName (string)

### media.chunks
- _id (ObjectId)
- files_id (ObjectId) -> 指向 media.files._id
- n (int) 分块序号
- data (binData) 分块数据
