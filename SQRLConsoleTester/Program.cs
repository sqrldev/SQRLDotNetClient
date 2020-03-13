using Sodium;
using SQRLUtilsLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SQRLUtilsLib.SQRLOptions;

namespace SQRLConsoleTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            SQRL sqrl = SQRL.GetInstance(true);

            
            Console.WriteLine("Importing Identity");
            var progress = new Progress<KeyValuePair<int,string>>(percent =>
            {
                Console.WriteLine($"{percent.Value}: {percent.Key}%");
            });

            SQRLIdentity newId = SQRLIdentity.FromFile(Path.Combine(@"C:\Users\jose\Downloads\SQRL-Test-Identity-Resources\", @"Spec-Vectors-Identity.sqrl"));
            //SQRLIdentity newId = SQRLIdentity.FromFile(Path.Combine(Directory.GetCurrentDirectory(), @"980591756918003626376697.sqrl"));

            SQRLOpts optsFlags = (sqrl.cps != null && sqrl.cps.Running ? SQRLOpts.SUK | SQRLOpts.CPS : SQRLOpts.SUK);


            SQRLOptions opts = new SQRLOptions(optsFlags);
            bool run =true;
            
           
            var block1Keys = await SQRL.DecryptBlock1(newId, "Zingo-Bingo-Slingo-Dingo", progress);
            if (block1Keys.DecryptionSucceeded)
            {
                try
                {
                    do
                    {
                        Console.WriteLine("Enter SQRL URL:");
                        string url = Console.ReadLine();
                        Uri requestURI = new Uri(url);
                        string AltID = "";


                        var siteKvp = SQRL.CreateSiteKey(requestURI, AltID, block1Keys.Imk);
                        Dictionary<byte[],Tuple<byte[],KeyPair>> priorKvps = null;
                        if(newId.Block3!=null && newId.Block3.Edition>0)
                        {
                            byte[] decryptedBlock3 = SQRL.DecryptBlock3(block1Keys.Imk, newId, out bool allGood);
                            List<byte[]> oldIUKs = new List<byte[]>();
                            if(allGood)
                            {
                                int skip = 0;
                                int ct = 0;
                                while (skip < decryptedBlock3.Length)
                                {
                                    oldIUKs.Add(decryptedBlock3.Skip(skip).Take(32).ToArray());
                                    skip += 32;
                                    ;
                                    if (++ct >= 3)
                                        break;
                                }
                                
                                SQRL.ZeroFillByteArray(ref decryptedBlock3);
                                priorKvps= SQRL.CreatePriorSiteKeys(oldIUKs, requestURI, AltID);
                                oldIUKs.Clear();
                            }
                        }

                        //SQRL.ZeroFillByteArray(ref decryptedData.Item2);
                        //decryptedData.Item2.ZeroFill();
                        var serverRespose = SQRL.GenerateQueryCommand(requestURI, siteKvp, opts,null,0, priorKvps);
                        
                        if (!serverRespose.CommandFailed)
                        {
                            
                            if (!serverRespose.CurrentIDMatch && !serverRespose.PreviousIDMatch)
                            {
                                StringBuilder additionalData = null;
                                if(!string.IsNullOrEmpty(serverRespose.SIN))
                                {
                                    additionalData = new StringBuilder();
                                    byte[] ids = SQRL.CreateIndexedSecret(requestURI, AltID, block1Keys.Imk, 
                                        Encoding.UTF8.GetBytes(serverRespose.SIN));
                                    additionalData.AppendLineWindows($"ins={Sodium.Utilities.BinaryToBase64(ids, Utilities.Base64Variant.UrlSafeNoPadding)}");
                                }
                                Console.WriteLine("The site doesn't recognize this ID, would you like to proceed and create one? (Y/N)");
                                if (Console.ReadLine().StartsWith("Y", StringComparison.OrdinalIgnoreCase))
                                {
                                    serverRespose = SQRL.GenerateNewIdentCommand(serverRespose.NewNutURL, siteKvp, 
                                        serverRespose.FullServerRequest, block1Keys.Ilk, opts, additionalData);
                                }
                            }
                            else if(serverRespose.PreviousIDMatch)
                            {
                                byte[] ursKey = null;
                                ursKey = SQRL.GetURSKey(serverRespose.PriorMatchedKey.Key, Sodium.Utilities.Base64ToBinary(
                                    serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));

                                StringBuilder additionalData = null;
                                if (!string.IsNullOrEmpty(serverRespose.SIN))
                                {
                                    additionalData = new StringBuilder();
                                    byte[] ids = SQRL.CreateIndexedSecret(requestURI, AltID, block1Keys.Imk, Encoding.UTF8.GetBytes(serverRespose.SIN));
                                    additionalData.AppendLineWindows($"ins={Sodium.Utilities.BinaryToBase64(ids, Utilities.Base64Variant.UrlSafeNoPadding)}");
                                    byte[] pids = SQRL.CreateIndexedSecret(serverRespose.PriorMatchedKey.Value.Item1, Encoding.UTF8.GetBytes(serverRespose.SIN));
                                    additionalData.AppendLineWindows($"pins={Sodium.Utilities.BinaryToBase64(pids, Utilities.Base64Variant.UrlSafeNoPadding)}");
                                    
                                }
                                serverRespose = SQRL.GenerateIdentCommandWithReplace(
                                    serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, block1Keys.Ilk, 
                                    ursKey,serverRespose.PriorMatchedKey.Value.Item2,opts, additionalData);
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
                                        var iukData = await SQRL.DecryptBlock2(newId, rescueCode, progress);
                                        if (iukData.DecryptionSucceeded)
                                        {
                                            byte[] ursKey = null;
                                            ursKey = SQRL.GetURSKey(iukData.Iuk, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                                            
                                            iukData.Iuk.ZeroFill();
                                            serverRespose = SQRL.GenerateEnableCommand(serverRespose.NewNutURL, siteKvp,serverRespose.FullServerRequest, ursKey,addClientData, opts);
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
                                Console.WriteLine("4- Re-Key ");
                                Console.WriteLine("10- Quit ");
                                Console.WriteLine("*********************************************");
                                var value = Console.ReadLine();
                                int.TryParse(value, out int selection);

                                switch (selection)
                                {
                                    case 0:
                                        {
                                            serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.ident, serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, addClientData, opts);
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
                                                serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.disable, serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, addClientData, opts);
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
                                            var iukData = await SQRL.DecryptBlock2(newId, rescueCode);
                                            if (iukData.DecryptionSucceeded)
                                            {
                                                byte[] ursKey = SQRL.GetURSKey(iukData.Iuk, Sodium.Utilities.Base64ToBinary(serverRespose.SUK, string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                                                iukData.Iuk.ZeroFill();
                                                serverRespose = SQRL.GenerateSQRLCommand(SQRLCommands.remove, serverRespose.NewNutURL, siteKvp, serverRespose.FullServerRequest, addClientData, opts,null,ursKey);
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
