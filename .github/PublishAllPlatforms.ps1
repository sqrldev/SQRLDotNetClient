

$token = "dfc69cb8ca709ad44452a12891f9504098baf115"
$milestone = "v0.1.2.0-beta"
$milestonedesc = "Version 0.1.2.0 Beta (Pre-Release)"  

#Navigate to our UI Client Folder
cd SQRLDotNetClientUI

echo "Building the Windows Client Release"
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true

echo "Building the Linux Client Release"
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true

echo "Building the OSX Client Release"
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=false /p:PublishTrimmed=true

#Navigate Back to Our Manin Solution Folder
cd ..
#Navigate Back to Our Installer Folder Folder
cd .\SQRLPlatformAwareInstaller\

#Building the Installer Binaries
echo "Building the Windows Platform Aware Installer Binnary"
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true

echo "Building the Linux Platform Aware Installer Binnary"
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true

echo "Building the OSX Platform Aware Installer Binnary"
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true

echo "Renaming the Installer with the platform _platform postfix"
Move-Item ".\bin\Release\netcoreapp3.1\osx-x64\publish\SQRLPlatformAwareInstaller" -Destination ".\bin\Release\netcoreapp3.1\osx-x64\publish\SQRLPlatformAwareInstaller_osx"  -Force
Move-Item ".\bin\Release\netcoreapp3.1\linux-x64\publish\SQRLPlatformAwareInstaller" -Destination ".\bin\Release\netcoreapp3.1\linux-x64\publish\SQRLPlatformAwareInstaller_linux"  -Force
Move-Item ".\bin\Release\netcoreapp3.1\win-x64\publish\SQRLPlatformAwareInstaller.exe" -Destination ".\bin\Release\netcoreapp3.1\win-x64\publish\SQRLPlatformAwareInstaller_win.exe" -Force

#Copying the installer into the regular client folder to be included in the zip
echo "Copying the Installer into the client folder to include in the release"
Copy-Item ".\bin\Release\netcoreapp3.1\osx-x64\publish\SQRLPlatformAwareInstaller_osx" -Destination "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\osx-x64\publish" -Force
Copy-Item ".\bin\Release\netcoreapp3.1\linux-x64\publish\SQRLPlatformAwareInstaller_linux" -Destination "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\linux-x64\publish" -Force
Copy-Item ".\bin\Release\netcoreapp3.1\win-x64\publish\SQRLPlatformAwareInstaller_win.exe" -Destination "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\win-x64\publish" -Force


#Create Temporary Directory to Publish into
mkdir "C:\Temp\SQRL\Publish\" -Force

#Zip Client Folder
echo "Zipping Linnux Client"
Compress-Archive -Path "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\linux-x64\publish\*" -CompressionLevel Optimal -DestinationPath "C:\Temp\SQRL\Publish\linux-x64.zip" -Force

echo "Zipping Windows Client"
Compress-Archive -Path "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\osx-x64\publish\*" -CompressionLevel Optimal -DestinationPath "C:\Temp\SQRL\Publish\osx-x64.zip" -Force

echo "Zipping OSX Client"
Compress-Archive -Path "..\SQRLDotNetClientUI\bin\Release\netcoreapp3.1\win-x64\publish\*" -CompressionLevel Optimal -DestinationPath "C:\Temp\SQRL\Publish\win-x64.zip" -Force


#Copying Platform Aware Installer (binary) to Publishing Folder
tar -cvzf C:\Temp\SQRL\Publish\SQRLPlatformAwareInstaller_osx.tar.gz -C .\bin\Release\netcoreapp3.1\osx-x64\publish\ ./SQRLPlatformAwareInstaller_osx
tar -cvzf C:\Temp\SQRL\Publish\SQRLPlatformAwareInstaller_linux.tar.gz -C .\bin\Release\netcoreapp3.1\linux-x64\publish\ ./SQRLPlatformAwareInstaller_linux
Copy-Item ".\bin\Release\netcoreapp3.1\win-x64\publish\SQRLPlatformAwareInstaller_win.exe" -Destination "C:\Temp\SQRL\Publish\" -Force

echo "Creating Github Release for Milestone: $milestone"
$releaseParams = 
@{
  "tag_name" = "$milestone"
  "target_commitish"= "master"
  "name"= "$milestone"
  "body"= "$milestonedesc"
  "draft" = $true
  "prerelease"= $true
}

$gitUrl= "https://api.github.com/repos/sqrldev/SQRLDotNetClient/releases"


$header = @{
 "Accept"="application/vnd.github.v3+json"
 "Authorization"="token $token"
 "Content-Type"="application/json"
} 


$newRelease= Invoke-WebRequest -Uri $gitUrl -Method Post -Body ($releaseParams|ConvertTo-Json) -ContentType "application/json" -Headers $header

echo "Release Created"


$jsonObject = ConvertFrom-Json $([String]::new($newRelease.Content))


Get-ChildItem "C:\Temp\SQRL\Publish"| 
#For each file in the publishing folder upload the asset
Foreach-Object {
    $contentType = If ($_.Extension -eq ".zip") {"application/x-gzip"} If ($_.Extension -eq ".gz") {"application/x-gzip"} else {"application/octet-stream"}
    $fileHeaders = @{
        "Accept"="application/vnd.github.v3+json"
        "Authorization"="token $token"
        "Content-Type"= $contentType
    }
    $fileName = $_.Name
    echo "Uploading File: $fileName"
    $uploadUrl = $jsonObject.upload_url.replace("{?name,label}","")
    $fileUrl = $uploadUrl+"?name="+$fileName
    
    $count=1
    $errror=$true
    while( $count -lt 3 -and $errror -eq $true)
    {
        try{
            $count = $count + 1
            $result =Invoke-RestMethod -Uri $fileUrl -Method Post -Headers $fileHeaders -InFile $_.FullName -ContentType $contentType -TimeoutSec 3600
            echo $result
            $errror=$false
        }
        catch {
            $errror=$true
               Write-Host "StatusCode:" $_.Exception.Response.StatusCode.value__ 
               Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
        }
    }

    #Take a break, the REST API is grumpy about less than 1 second posts
    Start-Sleep -s 10
}

echo "Release Creation Complete"