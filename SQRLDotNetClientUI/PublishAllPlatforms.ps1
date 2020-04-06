dotnet publish -r win-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true
cd ..
cd .\SQRLPlatformAwareInstaller\
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
Move-Item ".\bin\Release\netcoreapp3.1\osx-x64\publish\SQRLPlatformAwareInstaller" -Destination ".\bin\Release\netcoreapp3.1\osx-x64\publish\SQRLPlatformAwareInstaller_osx"  -Force
Move-Item ".\bin\Release\netcoreapp3.1\linux-x64\publish\SQRLPlatformAwareInstaller" -Destination ".\bin\Release\netcoreapp3.1\linux-x64\publish\SQRLPlatformAwareInstaller_linux"  -Force
Move-Item ".\bin\Release\netcoreapp3.1\win-x64\publish\SQRLPlatformAwareInstaller.exe" -Destination ".\bin\Release\netcoreapp3.1\win-x64\publish\SQRLPlatformAwareInstaller_win.exe" -Force
Copy-Item ".\bin\Release\netcoreapp3.1\osx-x64\publish\SQRLPlatformAwareInstaller_osx" -Destination "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\osx-x64\publish" -Force
Copy-Item ".\bin\Release\netcoreapp3.1\linux-x64\publish\SQRLPlatformAwareInstaller_linux" -Destination "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\linux-x64\publish" -Force
Copy-Item ".\bin\Release\netcoreapp3.1\win-x64\publish\SQRLPlatformAwareInstaller_win.exe" -Destination "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\win-x64\publish" -Force

mkdir "C:\Temp\SQRL\Publish\"
Compress-Archive -Path "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\linux-x64\publish\*" -CompressionLevel Optimal -DestinationPath "C:\Temp\SQRL\Publish\linux-x64.zip" -Force
Compress-Archive -Path "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\osx-x64\publish\*" -CompressionLevel Optimal -DestinationPath "C:\Temp\SQRL\Publish\osx-x64.zip" -Force
Compress-Archive -Path "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\win-x64\publish\*" -CompressionLevel Optimal -DestinationPath "C:\Temp\SQRL\Publish\win-x64.zip" -Force



Copy-Item ".\bin\Release\netcoreapp3.1\osx-x64\publish\SQRLPlatformAwareInstaller_osx" -Destination "C:\Temp\SQRL\Publish\" -Force
Copy-Item ".\bin\Release\netcoreapp3.1\linux-x64\publish\SQRLPlatformAwareInstaller_linux" -Destination "C:\Temp\SQRL\Publish\" -Force
Copy-Item ".\bin\Release\netcoreapp3.1\win-x64\publish\SQRLPlatformAwareInstaller_win.exe" -Destination "C:\Temp\SQRL\Publish\" -Force
