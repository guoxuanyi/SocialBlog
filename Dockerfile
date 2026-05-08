FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and all project files for layer-cached restore
COPY SocialBlog.sln ./
COPY SocialBlog.Api/SocialBlog.Api.csproj SocialBlog.Api/
COPY SocialBlog.Application/SocialBlog.Application.csproj SocialBlog.Application/
COPY SocialBlog.Core/SocialBlog.Core.csproj SocialBlog.Core/
COPY SocialBlog.Infrastructure/SocialBlog.Infrastructure.csproj SocialBlog.Infrastructure/

RUN dotnet restore

# Copy remaining source and publish
COPY . .
RUN dotnet publish SocialBlog.Api/SocialBlog.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

ENTRYPOINT ["dotnet", "SocialBlog.Api.dll"]
