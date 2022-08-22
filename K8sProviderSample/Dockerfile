#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["K8sProviderSample/K8sProviderSample.csproj", "K8sProviderSample/"]
RUN dotnet restore "K8sProviderSample/K8sProviderSample.csproj"
COPY . .
WORKDIR "/src/K8sProviderSample"
RUN dotnet build "K8sProviderSample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "K8sProviderSample.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "K8sProviderSample.dll"]