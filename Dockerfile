FROM microsoft/dotnet:sdk as build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY src/*.csproj ./
RUN dotnet restore

COPY src/ ./
RUN dotnet publish -c Release -o out

#TODO: Setup runtime image

ENTRYPOINT ["dotnet", "out/CommandStation.dll"]