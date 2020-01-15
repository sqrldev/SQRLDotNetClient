using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static SQRLUtilsLib.SQRLOptions;

namespace SQRLConsoleTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL(true);

            
            Console.WriteLine("Importing Identity");
            var progress = new Progress<KeyValuePair<int,string>>(percent =>
            {
                Console.WriteLine($"{percent.Value}: {percent.Key}%");
            });
            
            SQRLIdentity newId = SQRL.ImportSqrlIdentityFromFile(Path.Combine(Directory.GetCurrentDirectory(), @"Spec-Vectors-Identity.sqrl"));

            SQRLOpts optsFlags = (sqrl.cps != null && sqrl.cps.Running ? SQRLOpts.SUK | SQRLOpts.CPS : SQRLOpts.SUK);


            SQRLOptions opts = new SQRLOptions(optsFlags);
            bool run =true;
            
           
            var decryptedData = await sqrl.DecryptBlock1(newId, "Zingo-Bingo-Slingo-Dingo", progress);
            if (!decryptedData.Item1)
            {
                try
                {
                    do
                    {
                        Console.WriteLine("Enter SQRL URL:");
                        string url = Console.ReadLine();
                        Uri requestURI = new Uri(url);
                        string AltID = "";


                        var siteKvp = sqrl.CreateSiteKey(requestURI, AltID, decryptedData.Item2);
                        SQRL.ZeroFillByteArray(decryptedData.Item2);
                        var serverRespose = sqrl.GenerateQueryCommand(requestURI, siteKvp, opts);
                        if (!serverRespose.CommandFailed)
                        {
                            if (!serverRespose.CurrentIDMatch)
                            {
                                Console.WriteLine("The site doesn't recognize this ID, would you like to proceed and create one? (Y/N)");
                                if (Console.ReadLine().StartsWith("Y", StringComparison.OrdinalIgnoreCase))
                                {
                                    var sukvuk = sqrl.GetSukVuk(decryptedData.Item3);
                                    SQRL.ZeroFillByteArray(decryptedData.Item3);
                                    StringBuilder addClientData = new StringBuilder();
                                    addClientData.AppendLineWindows($"suk={Sodium.Utilities.BinaryToBase64(sukvuk.Key, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
                                    addClientData.AppendLineWindows($"vuk={Sodium.Utilities.BinaryToBase64(sukvuk.Value, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");

                                    serverRespose = sqrl.GenerateCommand(serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, "ident", opts, addClientData);
                                }
                            }
                            else if (serverRespose.CurrentIDMatch)
                            {
                                int askResponse = 0;
                                if (serverRespose.HasAsk)
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

                                StringBuilder addClientData = null;
                                if (askResponse > 0)
                                {
                                    addClientData = new StringBuilder();
                                    addClientData.AppendLineWindows($"btn={askResponse}");
                                }

                                if (serverRespose.SQRLDisabled)
                                {
                                    Console.WriteLine("SQRL Is Disabled, to Continue you must enable it. Do you want to? (Y/N)");
                                    if (Console.ReadLine().StartsWith("Y", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.WriteLine("Enter your Rescue Code (No Sapces or Dashes)");
                                        string rescueCode = Console.ReadLine().Trim();
                                         progress = new Progress<KeyValuePair<int, string>>(percent =>
                                        {
                                            Console.WriteLine($"Decrypting with Rescue Code: {percent.Key}%");
                                        });
                                        var iukData = await sqrl.DecryptBlock2(newId, rescueCode, progress);
                                        if (iukData.Item1)
                                        {
                                            byte[] ursKey = null;
                                            ursKey = sqrl.GetURSKey(iukData.Item2, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                                            SQRL.ZeroFillByteArray(iukData.Item2);
                                            serverRespose = sqrl.GenerateCommandWithURS(serverRespose.NewNutURL, siteKvp, ursKey, serverRespose.FullServerRequest, "enable", opts, null);
                                        }
                                        else
                                        {
                                            throw new Exception("Failed to Decrypt Block 2, Invalid Rescue Code");
                                        }

                                    }
                                }

                                Console.WriteLine("Which Command Would you like to Issue?:");
                                Console.WriteLine("*********************************************");
                                Console.WriteLine("0- Ident (Default/Enter) ");
                                Console.WriteLine("1- Disable ");
                                Console.WriteLine("2- Remove ");
                                Console.WriteLine("3- Cancel ");
                                Console.WriteLine("10- Quit ");
                                Console.WriteLine("*********************************************");
                                var value = Console.ReadLine();
                                int.TryParse(value, out int selection);

                                switch (selection)
                                {
                                    case 0:
                                        {
                                            serverRespose = sqrl.GenerateCommand(serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, "ident", opts, addClientData);
                                            if (sqrl.cps != null && sqrl.cps.PendingResponse)
                                            {
                                                sqrl.cps.cpsBC.Add(new Uri(serverRespose.SuccessUrl));
                                            }
                                        }
                                        break;
                                    case 1:
                                        {
                                            Console.WriteLine("This will disable all use of this SQRL Identity on the server, are you sure you want to proceed?: (Y/N)");
                                            if (Console.ReadLine().StartsWith("Y", StringComparison.OrdinalIgnoreCase))
                                            {
                                                serverRespose = sqrl.GenerateCommand(serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, "disable", opts, addClientData);
                                                if (sqrl.cps != null && sqrl.cps.PendingResponse)
                                                {
                                                    sqrl.cps.cpsBC.Add(sqrl.cps.Can);
                                                }
                                            }

                                        }
                                        break;
                                    case 2:
                                        {
                                            Console.WriteLine("Enter your Rescue Code (No Sapces or Dashes)");
                                            string rescueCode = Console.ReadLine().Trim();
                                            progress = new Progress<KeyValuePair<int, string>>(percent =>
                                            {
                                                Console.WriteLine($"Decrypting with Rescue Code: {percent.Key}%");
                                            });
                                            var iukData = await sqrl.DecryptBlock2(newId, rescueCode);
                                            if (iukData.Item1)
                                            {
                                                byte[] ursKey = sqrl.GetURSKey(iukData.Item2, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                                                SQRL.ZeroFillByteArray(iukData.Item2);
                                                serverRespose = sqrl.GenerateCommandWithURS(serverRespose.NewNutURL, siteKvp, ursKey, serverRespose.FullServerRequest, "remove", opts, null);
                                                if (sqrl.cps != null && sqrl.cps.PendingResponse)
                                                {
                                                    sqrl.cps.cpsBC.Add(sqrl.cps.Can);
                                                }
                                            }
                                            else
                                                throw new Exception("Failed to Decrypt Block 2, Invalid Rescue Code");
                                        }
                                        break;
                                    case 3:
                                        {
                                            if (sqrl.cps != null && sqrl.cps.PendingResponse)
                                            {
                                                sqrl.cps.cpsBC.Add(sqrl.cps.Can);
                                            }
                                        }
                                        break;
                                    default:
                                        {
                                            Console.WriteLine("bye");
                                            run = false;
                                        }
                                        break;
                                }
                            }
                        }
                    } while (run);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error: {ex.ToString()}");
                }
            }
            else
            {
                Console.WriteLine("Failed to Decrypt Block 1, bad password!");
            }
        }
    }
}
