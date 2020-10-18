# Build compile image including sdk
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy everything and build
COPY . ./
RUN dotnet publish UltimateTrivia/UltimateTrivia.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "UltimateTrivia.dll"]