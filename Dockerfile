# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY ./*.sln ./
COPY OnlineAuction/*.csproj ./OnlineAuction/
RUN dotnet restore

# Copy the rest of the code
COPY . ./
WORKDIR /app/OnlineAuction
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/OnlineAuction/out ./
ENTRYPOINT ["dotnet", "OnlineAuction.dll"] 