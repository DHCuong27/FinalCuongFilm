# 1. Dùng .NET 8 SDK để build code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ solution vào
COPY . .

# Restore và Publish đích danh project MVC
RUN dotnet restore "FinalCuongFilm.MVC/FinalCuongFilm.MVC.csproj"
RUN dotnet publish "FinalCuongFilm.MVC/FinalCuongFilm.MVC.csproj" -c Release -o /app

# 2. Dùng .NET 8 Runtime để chạy web
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# 3. Cài đặt FFmpeg để Hangfire dùng nén video
RUN apt-get update && apt-get install -y ffmpeg

# 4. Lệnh khởi động web
ENTRYPOINT ["dotnet", "FinalCuongFilm.MVC.dll"]