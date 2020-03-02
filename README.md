
# SQRL .Net Core Client and Library
This project has 2 main parts: A **SQRL client** and a **SQRL library**. Below is information on both. 

[SQRL Dot Net Core Client](#SQRL-Dot-Net-Core-Client) <br/>
[SQRL Dot Net Core Library](#SQRL-Dot-Net-Core-Library)

### SQRL Dot Net Core Client

An implementation of a fully-featured SQRL client, along with a cross-platform user interface (using the Avalonia UI framework).

#### Installing the Client on Linux (desktop environment / xdg-desktop is required)

```shell
sudo apt-get install -y libgdiplus
wget https://raw.githubusercontent.com/sqrldev/SQRLDotNetClient/master/Installers/Linux/SQRL.sh
chmod a+x SQRL.sh
./SQRL.sh
```
#### Installing Client on MacOSX

###### Install Pre-Reqs

```shell
/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"

brew install mono-libgdiplus
```



- Download File https://client.sqrloauth.com/osx-x64/SQRLHelper.app.tar.bz2
- Double Click on the File this will expand it and will extract the SQRLHelper app
- Double Click the SQRL Helper App to download the SQRL Client
- This may take a few minutes to download and setup

#### Install Client on Windows OS

- Download the latest Windows Client binary from the Github releases: [https://github.com/sqrldev/SQRLDotNetClient/releases](https://github.com/sqrldev/SQRLDotNetClient/releases)
- Save the file in the `C:\SQRL\` folder
- Download the registry file from: https://github.com/sqrldev/SQRLDotNetClient/blob/master/Installers/Windows/WindowsRegEx.reg
- Run the downloaded file to register the sqrl schema. If your Client isn't in the `C:\SQRL` folder, you'll have to adjust the script.

![](/SQRLDotNetClientUI/Assets/SQRL_InAction.gif)



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

//Returns a tuple of 3 values a boolean indicating sucess, Item2 = IMK Item3 = ILK
var block1DecryptedData = await SQRL.DecryptBlock1(existingIdentity, "My-Awesome-Password", progress);

/*
Note that bloc1DecryptedData returns a tuple as mentioned above 
Item1 is a  boolean (sucess/not)
Item2 is (IMK) Identity Master Key
Item3 is (ILK) Identity Lock Key
*/
if (block1DecryptedData.Item1) //If success
{
    //This is the site's Key-Pair for signing requests
	Sodium.KeyPair siteKP = SQRL.CreateSiteKey(new Uri("sqrl://sqrl.grc.com/cli.sqrl?nut=fXkb4MBToCm7"), "Alt-ID-If-You-Want-One", block1DecryptedData.Item2); //Item2=IMK
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
var serverRespose = SQRL.GenerateQueryCommand(requestURI, siteKvp, opts,null,0, priorKvps);
```


##### Deal with "Ask" on query response

```csharp
if (serverRespose.HasAsk) //Returns true if server sent "Ask"
{
	Console.WriteLine(serverRespose.AskMessage);
	Console.WriteLine($"Enter 1 for {serverRespose.GetAskButtons[0]} or 2 for 	{serverRespose.GetAskButtons[1]}");
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

StringBuilder addClientData = null;
if (askResponse > 0)
{
    addClientData = new StringBuilder();
    addClientData.AppendLineWindows($"btn={askResponse}");
}
// addClientData now needs to be passed in to the next command (Ident)
```
##### Generate "Ident" (create) command

Assumes you have a generated SiteKeyPair.
Assumes you have a decrypted ILK (Identity Lock Key) (by decrypting block1).

```csharp
if (!serverRespose.CurrentIDMatch) //New Account
{
    //Generates a new identity with a new SUK/VUK generated from the decrypted block1
    serverRespose = SQRL.GenerateNewIdentCommand(serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, decryptedData.Item3, opts);
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
        var iukData = await SQRL.DecryptBlock2(newId, rescueCode, progress);
        if (iukData.Item1)
        {
            byte[] ursKey = null;
            ursKey = SQRL.GetURSKey(iukData.Item2, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));

            iukData.Item2.ZeroFill();
            serverRespose = SQRL.GenerateEnableCommand(serverRespose.NewNutURL, siteKvp,serverRespose.FullServerRequest, ursKey,addClientData, opts);
        }
        else
        {
            throw new Exception("Failed to Decrypt Block 2, Invalid Rescue Code");
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
    serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.disable, serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, addClientData, opts);
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

Console.WriteLine("Enter your Rescue Code (No Sapces or Dashes)");
string rescueCode = Console.ReadLine().Trim();
progress = new Progress<KeyValuePair<int, string>>(percent =>
{
    Console.WriteLine($"Decrypting with Rescue Code: {percent.Key}%");
});
var iukData = await SQRL.DecryptBlock2(newId, rescueCode);
if (iukData.Item1)
{
    byte[] ursKey = SQRL.GetURSKey(iukData.Item2, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
    iukData.Item2.ZeroFill();
    serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.remove, serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, addClientData, opts,null,ursKey);
    if (sqrlInstance.cps != null && sqrlInstance.cps.PendingResponse)
    {
        sqrlInstance.cps.cpsBC.Add(sqrlInstance.cps.Can);
    }
}
else
    throw new Exception("Failed to decrypt Block 2, invalid Rescue Code");
```

##### Send "Ident" with "Replace" (prior identity matched)

Sends an "Ident" command along with new SUK/VUK and prior URS to replace identity.

```csharp
if(serverRespose.PreviousIDMatch)
{                            
    byte[] ursKey = null;
    ursKey = SQRL.GetURSKey(serverRespose.PriorMatchedKey.Key, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
    serverRespose = SQRL.GenerateIdentCommandWithReplace(serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, decryptedData.Item3,ursKey,serverRespose.PriorMatchedKey.Value,opts);
}
```
##### Send "Ident" and deal with CPS

Any serverResponse can be dealt with via CPS, if CPS is enabled and has a "pendingRequest".

```csharp
// Instantiate the sqrl library to get the 
// CPS server functionality
SQRL sqrlInstance = SQRL.GetInstance(true);

var serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.ident, serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, addClientData, opts);
if (sqrlInstance.cps != null && sqrlInstance.cps.PendingResponse)
{
    sqrlInstance.cps.cpsBC.Add(new Uri(serverRespose.SuccessUrl));
}
```

