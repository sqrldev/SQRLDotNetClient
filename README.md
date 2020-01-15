# SQRL Dot Net Core Client and Library
This project has 2 main parts a SQRL library and a SQRL Client below is information on both.

### SQRL Dot Net Core Library

An implementation of the full client protocol for SQRL written in Dot Net Core fully cross-platform to (Win, Nix, Mac)

#### How to Install

`Install-Package SQRLClientLib` 

#### Requirements

To use this library you will need Sodium.Core.ForSqrl package (can also be installed from nuget)
This is a Dot Net Core 3.1 library so you will need a compatible project

#### How to Use

Create an Instance of the SQRL library

```charp
//the boolean here tells the library to start the CPS server
SQRLUtilsLib.SQRL sqrlLib = new SQRLUtilsLib.SQRL(true); 
```



### SQRL Dot Net Core Client

An implementation of a full SQRL client along with a cross-platform UI using Avalonia

