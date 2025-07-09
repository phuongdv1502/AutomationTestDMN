FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DmnTester.Api/DmnTester.Api.csproj", "DmnTester.Api/"]
COPY ["DmnTester.Core/DmnTester.Core.csproj", "DmnTester.Core/"]
RUN dotnet restore "DmnTester.Api/DmnTester.Api.csproj"
COPY . .
WORKDIR "/src/DmnTester.Api"
RUN dotnet build "DmnTester.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DmnTester.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DmnTester.Api.dll"] 