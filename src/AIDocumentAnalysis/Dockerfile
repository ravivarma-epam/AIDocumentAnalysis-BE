FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/AIDocumentAnalysis/AIDocumentAnalysis.csproj", "AIDocumentAnalysis/"]
RUN dotnet restore "src/AIDocumentAnalysis/AIDocumentAnalysis.csproj"
COPY . .
WORKDIR "/src/AIDocumentAnalysis"
RUN dotnet build "AIDocumentAnalysis.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AIDocumentAnalysis.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIDocumentAnalysis.dll"]
