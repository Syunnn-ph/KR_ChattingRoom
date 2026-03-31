# ================================
# 階段一：Build
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製 .sln 與所有專案檔
COPY . .

# 還原套件
RUN dotnet restore

# 發佈 Release 版本
# ⚠️  把 "YourProjectName" 換成你的主要 Web API 專案名稱（資料夾名稱）
RUN dotnet publish "WebApplication1/WebApplication1.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ================================
# 階段二：Runtime（最終映像檔）
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 複製發佈結果
COPY --from=build /app/publish .

# Render 會自動注入 PORT 環境變數，這裡讓 ASP.NET 監聽它
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

EXPOSE 8080

# ⚠️  把 "YourProjectName" 換成你的主要 Web API 專案名稱
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
