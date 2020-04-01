dotnet publish -r win-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true

