# 1️⃣ Build阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 复制 csproj 并 restore
COPY PaymentService.Api.csproj ./
RUN dotnet restore

# 复制全部代码
COPY . ./

# 发布
RUN dotnet publish -c Release -o /app/out

# 2️⃣ Runtime阶段
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 复制发布结果
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "PaymentService.Api.dll"]