# 1. Dùng .NET 8 SDK để build code
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies first so Docker can reuse this layer when source files change.
COPY *.sln ./
COPY global.json ./
COPY FinalCuongFilm.MVC/*.csproj FinalCuongFilm.MVC/
COPY FinalCuongFilm.ApplicationCore/*.csproj FinalCuongFilm.ApplicationCore/
COPY FinalCuongFilm.Datalayer/*.csproj FinalCuongFilm.Datalayer/
COPY FinalCuongFilm.Service/*.csproj FinalCuongFilm.Service/
COPY FinalCuongFilm.Common/*.csproj FinalCuongFilm.Common/
RUN dotnet restore "FinalCuongFilm.MVC/FinalCuongFilm.MVC.csproj"

COPY . .
RUN dotnet publish "FinalCuongFilm.MVC/FinalCuongFilm.MVC.csproj" -c Release -o /app --no-restore

# 2. Dùng .NET 8 Runtime để chạy web
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# 3. Cài đặt FFmpeg để Hangfire dùng nén video
RUN apt-get update \
 && apt-get install -y --no-install-recommends ffmpeg \
 && rm -rf /var/lib/apt/lists/*


# Railway routes traffic to the port exposed by the container.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Dòng cuối cùng giữ nguyên
ENTRYPOINT ["dotnet", "FinalCuongFilm.MVC.dll"]
