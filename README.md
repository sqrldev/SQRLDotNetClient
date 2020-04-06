
# SQRL .Net Core Client and Library
This project has 2 main parts: A **SQRL client** and a **SQRL library**. Below is information on both. 

[SQRL Dot Net Core Client](#SQRL-Dot-Net-Core-Client) <br/>
[SQRL Dot Net Core Library](#SQRL-Dot-Net-Core-Library)

### SQRL Dot Net Core Client

An implementation of a fully-featured SQRL client, along with a cross-platform user interface (using the Avalonia UI framework).



#### Installing the Client on Linux (desktop environment / xdg-desktop is required)

```shell
sudo apt-get install -y libgdiplus
```


- Go to the Latest Releases Page at: https://github.com/sqrldev/SQRLDotNetClient/releases
- Select Assets
- Download SQRLPlatformAwareInstaller_linux
  ![image-20200406102440688](/SQRLDotNetClientUI/Assets/Linux_Installer.png)
- Open Terminal and CD into your Download Location

```shell
chmod a+x ./SQRLPlatformAwareInstaller_linux
./SQRLPlatformAwareInstaller_linux
```

- Follow the Installer Prompts

#### Installing Client on MacOSX

- Open Terminal

```shell
/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"

brew install mono-libgdiplus
```

- Go to the Latest Releases at: https://github.com/sqrldev/SQRLDotNetClient/releases

- Select Assets

- Download: SQRLPlatformAwareInstaller_osx

  ![image-20200406102814087](/SQRLDotNetClientUI/Assets/MacOsx_Installer.png)

- Open Terminal

- CD into the Downloads Folder

  ```shell
  chmod a+x ./SQRLPlatformAwareInstaller_osx.dms
  ./SQRLPlatformAwareInstaller_osx.dms
  ```

- You may get a security prompt at this point about unknown developer
  ![image-20200406103148991](/SQRLDotNetClientUI/Assets/Mac_Error1.png)

- Click Cancel

- Open System Preferences and Click Security and Privacy
  ![image-20200406103317325](/SQRLDotNetClientUI/Assets/Mac_SecurityAndPrivacy.png)

- You should see a message at the bottom regarding Blocked SQRL and a button that says Allow Anyways click that
  ![image-20200406103421405](/SQRLDotNetClientUI/Assets/image-20200406103421405.png)

- Go back to the Terminal and Launch the Installer again

  ```shell
  ./SQRLPlatformAwareInstaller_osx.dms
  ```

- This time, on the message, click Open

  ![image-20200406103539966](/SQRLDotNetClientUI/Assets/MacOsx_Error2.png)

- Follow the Installer Prompts
  ![image-20200406103648963](/SQRLDotNetClientUI/Assets/MacOsx_InstallerPrompt.png)

#### Install Client on Windows OS

- Download the latest Windows Client binary from the Github releases: [https://github.com/sqrldev/SQRLDotNetClient/releases](https://github.com/sqrldev/SQRLDotNetClient/releases)
- Download: SQRLPlatformAwareInstaller_win.exe
  ![image-20200406103803339](/SQRLDotNetClientUI/Assets/WinInstaller.png)
- Run SQRLPlatformAwareInstaller_win.exe
- If Prompted by UAC Click Run Anyway
  ![image-20200406103923048](/SQRLDotNetClientUI/Assets/WinRunAnyways.png)
- Follow Installer Prompts
  ![image-20200406104037042](/SQRLDotNetClientUI/Assets/WinInstallerPrompt.png)

![](/SQRLDotNetClientUI/Assets/NewIdentitySetup.gif)



### SQRL Dot Net Core Library

An implementation of the full client protocol for SQRL written in .Net Core, fully cross-platform too (Win, Nix, Mac).

![SQRLClientDemo](/SQRLUtilsLib/Resources/SQRLClientDemo.gif)

#### How to Install

`Install-Package SQRLClientLib` 

#### Requirements

This is a .Net Core 3.1 library so you will need a compatible project.

#### How to Use

Almost all of the library's functionality can be accessed by simply calling the static methods of the `SQRL` class:

```csharp
/* Import the library's namespace */
using SQRLUtilsLib;

/* 
There is no need to instanciate the library 
if no CPS server is needed. Just call the 
static functions of the SQRL class.
*/ 
var rc = SQRL.CreateRescueCode(); 
```

##### Create an instance of the SQRL class

```csharp
/* 
Create an instance of the SQRL library only if 
you need the CPS server functionality. The SQRL 
class follows the singleton pattern, so instead
of calling the constructor, you call the library's
GetInstance method, passing in true if you want
to immediately start the CPS server, or false
otherwise.
*/ 
SQRL sqrlLib = SQRL.GetInstance(true); 
```

##### Create a new SQRL identity (from scratch)

```csharp
//Creates a new Identity object
SQRLIdentity newIdentity = new SQRLUtilsLib.SQRLIdentity();

//Generates a Identity Unlock Key
var iuk = SQRL.CreateIUK();

// Generaties a Rescue Code
var rescueCode = SQRL.CreateRescueCode();

// Used to report progress when encrypting / decrypting (progress bar maybe)
var progress = new Progress<KeyValuePair<int, string>>(percent =>
{
	Console.WriteLine($"{percent.Value}: {percent.Key}%");
});

newIdentity = await SQRL.GenerateIdentityBlock1(iuk, "My-Awesome-Password", newIdentity, progress);

newIdentity = await SQRL.GenerateIdentityBlock2(iuk, rescueCode, newIdentity, progress);
```
##### Import identity from file

```csharp
SQRLIdentity newIdentity = SQRLIdentity.FromFile(@"C:\Temp\identiy.sqrl");
```



##### Import identity from text

```csharp
//Creates a new Identity object
string identityTxt = "KKcC 3BaX akxc Xwbf xki7 k7mF GHhg jQes gzWd 6TrK vMsZ dBtB pZbC zsz8 cUWj DtS2 ZK2s ZdAQ 8Yx3 iDyt QuXt CkTC y6gc qG8n Xfj9 bHDA 422";

string rescueCode = "119887487132283883187570";

string password = "Zingo-Bingo-Slingo-Dingo";        

//Reports progress while decrypting / encrypting the identity
var progress = new Progress<KeyValuePair<int, string>>(percent =>
{
	Console.WriteLine($"{percent.Value}: {percent.Key}%");
});

// Decodes the identity from text import
SQRLIdentity newIdentity = await SQRL.DecodeSqrlIdentityFromText(identityTxt, rescueCode, password, progress);
```
##### Export identity to file

```csharp
newIdentity.WriteToFile(@"C:\Temp\My-SQRL-Identity.sqrl");
```

##### Re-Key identity

```csharp
//Have an existing Identity object (somehow)
SQRLIdentity existingIdentity = ...

//Reports progress while decrypting / encrypting the identity it is optional
var progress = new Progress<KeyValuePair<int, string>>(percent =>
{
	Console.WriteLine($"{percent.Value}: {percent.Key}%");
});
//Re-Keys the existing identity object and returns a tuple of your new rescue code and the new identity (which now contains a new entry in block3 )
var reKeyResponse = await SQRL.RekeyIdentity(existingIdentity, rescueCode, "My-New-Even-Better-Password", progress); 

Console.WriteLine($"New Rescue Code: {reKeyResponse.Key}");

var NewlyReKeyedIdentity = reKeyResponse.Value;
```


##### Generate a Site Key-Pair

```csharp
//Reports progress while decrypting / encrypting the identity
var progress = new Progress<KeyValuePair<int, string>>(percent =>
{
	Console.WriteLine($"{percent.Value}: {percent.Key}%");
});
//Have an existing identity (somehow)
SQRLIdentity existingIdentity = ...

//Returns an object containing a boolean sucess indicator, the IMK and the ILK
var block1DecryptedData = await SQRL.DecryptBlock1(existingIdentity, "My-Awesome-Password", progress);

if (block1DecryptedData.DecryptionSucceeded)
{
    //This is the site's Key-Pair for signing requests
	Sodium.KeyPair siteKP = SQRL.CreateSiteKey(
        new Uri("sqrl://sqrl.grc.com/cli.sqrl?nut=fXkb4MBToCm7"), "Alt-ID-If-You-Want-One", block1DecryptedData.Imk);
}
else
	throw new Exception("Invalid password, failed to decrypt");
```


##### Generate a query command to the server

Assumes you have a valid SiteKeyPair

```csharp
//SQRL url
Uri sqrlUrl = new Uri("sqrl://sqrl.grc.com/cli.sqrl?nut=fXkb4MBToCm7");
//SQRL client options include CPS, SUK, HARDLOCK, NOIPTEST,SQRLONLY
SQRLOptions opts = new SQRLOptions(SQRLOptions.SQRLOpts.CPS | SQRLOptions.SQRLOpts.SUK | SQRLOptions.SQRLOpts.);            
/*
Generates a query command and sends it to the server, requires that you have a  valid site keypair.
Returns a "SQRLServerResponse" object which contains all pertinent data of the response from the server.
*/
var serverRespose = SQRL.GenerateQueryCommand(requestURI, siteKeyPair, opts, null, 0, priorSiteKeyPairs);
```


##### Deal with "Ask" on query response

```csharp
if (serverRespose.HasAsk) //Returns true if server sent "Ask"
{
	Console.WriteLine(serverRespose.AskMessage);
	Console.WriteLine($"Enter 1 for {serverRespose.GetAskButtons[0]} or 2 for {serverRespose.GetAskButtons[1]}");
	int resp;
    do
    {
        string response = Console.ReadLine();
        int.TryParse(response, out resp);
        if (resp == 0)
        {
            Console.WriteLine("Invalid Entry, please enter 1 or 2 as shown above");
        }

    } while (resp == 0);

    askResponse = resp;
}

StringBuilder additionalClientData = null;
if (askResponse > 0)
{
    additionalClientData = new StringBuilder();
    additionalClientData.AppendLineWindows($"btn={askResponse}");
}
// additionalClientData now needs to be passed in to the next command (Ident)
```

##### Generate "Ident" (create) command

Assumes you have a generated SiteKeyPair.
Assumes you have a decrypted ILK (Identity Lock Key) by decrypting block1.

```csharp
if (!serverRespose.CurrentIDMatch) //New Account
{
    //Generates a new identity with a new SUK/VUK generated from the decrypted block1
    serverRespose = SQRL.GenerateNewIdentCommand(serverRespose.NewNutURL, siteKeyPair, serverRespose.FullServerRequest, decryptedData.Ilk, opts);
}
```

****



##### Send "Enable" command

```csharp
if (serverRespose.SQRLDisabled)
{
    Console.WriteLine("SQRL is disabled, to continue you must enable it. Do you want to? (Y/N)");
    if (Console.ReadLine().StartsWith("Y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Enter your Rescue Code (no sapces or dashes)");
        string rescueCode = Console.ReadLine().Trim();
        progress = new Progress<KeyValuePair<int, string>>(percent =>
        {
            Console.WriteLine($"Decrypting with Rescue Code: {percent.Key}%");
        });
        var decryptionResult = await SQRL.DecryptBlock2(newId, rescueCode, progress);
        if (decryptionResult.DecryptionSucceeded)
        {
            byte[] ursKey = null;
            ursKey = SQRL.GetURSKey(decryptionResult.Iuk, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
            decryptionResult.Iuk.ZeroFill(); // Overwrite IUK so that we leave no traces of it in RAM
            serverRespose = SQRL.GenerateEnableCommand(serverRespose.NewNutURL, siteKeyPair, serverRespose.FullServerRequest, ursKey, additionalClientData, opts);
        }
        else
        {
            throw new Exception("Failed to decrypt block 2, invalid rescue code");
        }
    }
}
```
##### Send "Disable" command

```csharp
// Instantiate the sqrl library to get the 
// CPS server functionality
SQRL sqrlInstance = SQRL.GetInstance(true);

Console.WriteLine("This will disable all use of this SQRL identity on the server, are you sure you want to proceed?: (Y/N)");
if (Console.ReadLine().StartsWith("Y", StringComparison.OrdinalIgnoreCase))
{
    serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.disable, serverRespose.NewNutURL, siteKeyPair, serverRespose.FullServerRequest, additionalClientData, opts);
    if (sqrlInstance.cps != null && sqrlInstance.cps.PendingResponse)
    {
        sqrlInstance.cps.cpsBC.Add(sqrlInstance.cps.Can);
    }
}
```

##### Send "Remove" command

```csharp
// Instantiate the sqrl library to get the 
// CPS server functionality
SQRL sqrlInstance = SQRL.GetInstance(true);

Console.WriteLine("Enter your rescue code (no sapces or dashes)");
string rescueCode = Console.ReadLine().Trim();
progress = new Progress<KeyValuePair<int, string>>(percent =>
{
    Console.WriteLine($"Decrypting with rescue code: {percent.Key}%");
});
var decryptionResult = await SQRL.DecryptBlock2(newId, rescueCode);
if (decryptionResult.DecryptionSucceeded)
{
    byte[] ursKey = SQRL.GetURSKey(decryptionResult.Iuk, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
    decryptionResult.Iuk.ZeroFill(); // Overwrite IUK so that we leave no traces of it in RAM
    serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.remove, serverRespose.NewNutURL, siteKeyPair, serverRespose.FullServerRequest, additionalClientData, opts, null, ursKey);
    if (sqrlInstance.cps != null && sqrlInstance.cps.PendingResponse)
    {
        sqrlInstance.cps.cpsBC.Add(sqrlInstance.cps.Can);
    }
}
else
    throw new Exception("Failed to decrypt block 2, invalid rescue code");
```

##### Send "Ident" with "Replace" (prior identity matched)

Sends an "Ident" command along with new SUK/VUK and prior URS to replace identity.

```csharp
if(serverRespose.PreviousIDMatch)
{                            
    byte[] ursKey = null;
    ursKey = SQRL.GetURSKey(serverRespose.PriorMatchedKey.Key, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
    serverRespose = SQRL.GenerateIdentCommandWithReplace(serverRespose.NewNutURL, siteKeyPair, serverRespose.FullServerRequest, decryptedData.Ilk, ursKey, serverRespose.PriorMatchedKey.KeyPair, opts);
}
```
##### Send "Ident" and deal with CPS

Any serverResponse can be dealt with via CPS, if CPS is enabled and has a "pendingRequest".

```csharp
// Instantiate the sqrl library to get the 
// CPS server functionality
SQRL sqrlInstance = SQRL.GetInstance(true);

var serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.ident, serverRespose.NewNutURL, siteKeyPair, serverRespose.FullServerRequest, additionalClientData, opts);
if (sqrlInstance.cps != null && sqrlInstance.cps.PendingResponse)
{
    sqrlInstance.cps.cpsBC.Add(new Uri(serverRespose.SuccessUrl));
}
```

