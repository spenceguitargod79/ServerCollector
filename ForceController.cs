using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace HydraMVC.Controllers
{
    public class ForceController : Controller
    {

        public static bool forceInStateAlready = false;
        //
        // GET: /Force/
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForceActivate(string ipAddress, int choice)
        {
            string result = IsForceOn(ipAddress);
            if (choice == 1 && result.Equals("OFF"))//cover scenerio where force was manually turned on via server, after the Hydra gui was loaded when force was off
            {
                StartForce(ipAddress);
                string subject = "Force Activated on " + ipAddress;
                string body = "The force has been turned on by " + User.Identity.GetUserName() + " on " + ipAddress;
                string emailTo = "sqa@playags.com";
                MorpheusController.SendEmail(subject, body, emailTo);
            }
            else if (choice == 2 && result.Equals("ON"))
            {
                StopForce(ipAddress);
                string subject = "Force turned OFF on " + ipAddress;
                string body = "The force has been turned OFF by " + User.Identity.GetUserName() + " on " + ipAddress;
                string emailTo = "sqa@playags.com";
                MorpheusController.SendEmail(subject, body, emailTo);
            }
            else
            {
                Trace.WriteLine("ERROR: invalid choice parameter sent to ForceActivate() in ForceController - FORCE is already ON or OFF");
                //string subject = "FORCE ERROR on " + ipAddress;
                //string body = "An attempt to turn the force on or off was made by " + User.Identity.GetUserName() +
                //    " on " + ipAddress + "- The force may have already been activated or deactivated directly on the server." +
                //    " Contact one of the following active users: " + MorpheusController.GetActiveUsers(ipAddress);
                //string emailTo = User.Identity.GetUserName()  + "@playags.com";
                //MorpheusController.SendEmail(subject, body, emailTo);
                forceInStateAlready = true;
            }

            return View();
        }

        public static void ResetForceInStateAlready()
        {
            forceInStateAlready = false;
        }

        public static bool GetForceInStateAlready()
        {
            return forceInStateAlready;
        }

        public static string scriptPath = @"C:\hydra\HydraLive\trunk\Scripts\";
        public static string forcePath = @"FOLGS\";
        
        //First register the server's MAC address (physical address) with FOLGS - 'ip config -all' on desired server
        //Manually do this via the gui link - http://secure-service/SecurityService/FOLGS

        //Copy/paste folder onto a server - directly on C drive for testing
        public static string CopyForceFilesToServer(string ip)
        {
            string fileName = "";
            string pathCopyFrom = @"\\localhost\C$\hydra\HydraLive\trunk\Scripts\ForcedOutcome";//source path
            string pathCopyTo = @"\\" + ip + @"\C$\"+forcePath;//target path
            string destFile = System.IO.Path.Combine(pathCopyTo, fileName);

            // Create a new target folder, if necessary.
            if (!System.IO.Directory.Exists(pathCopyTo))
            {
                System.IO.Directory.CreateDirectory(pathCopyTo);
            }

            //System.IO.File.Copy(pathCopyFrom, pathCopyTo, true);//copy the forcefiles folder from this machine to desired server

            if (System.IO.Directory.Exists(pathCopyFrom))
            {
                string[] files = System.IO.Directory.GetFiles(pathCopyFrom);

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    fileName = System.IO.Path.GetFileName(s);
                    destFile = System.IO.Path.Combine(pathCopyTo, fileName);
                    System.IO.File.Copy(s, destFile, true);
                }
            }
            else
            {
                Trace.WriteLine("Source path does not exist!");
            }

            return "";
        }

        //Start the force - Enable-FOLGS batch file
        //ISSUE: The LGS cmd window doesnt stay open, which causes LGS service to 'exit'
        //It works fine when running
        public static string StartForce(string ip)
        {
            if (ip != null)
            {
                //Kill notepads
                KillALLFO(ip);
                //Copy force files over to server
                CopyForceFilesToServer(ip);
                //give host file access to mothership
                AppendToHostsFileOnServer(ip);                
                //string serverPath = @"C:\hydra\HydraLive\trunk\Scripts\";//Location where the start service bat will end up
                //-s -i flags run as a system user and interactive window, so the LGS window doesn't close unexpectedly
                MorpheusController.CreateBatchFile("C:\\PSTools\\psexec.exe \\\\" + ip + " -s -i \"C:\\"+forcePath+"Enable-FOLGS.bat", scriptPath + @"FORCE-ON.bat");
                MorpheusController.RunBatchFile(scriptPath + @"FORCE-ON.bat", ip);
                //Need to manually run NET START EGSSERVICES
                MorpheusController.CreateBatchFile("NET START EGSSERVICES", scriptPath + @"START-SERVICES.bat");
                //Copy the bat file to server
                CopyStartServiceToServer(ip);
                //Run the file
                MorpheusController.RunBatchFile(scriptPath + @"START-SERVICES.bat", ip);
            }

            return "";
        }       

        public static string CopyStartServiceToServer(string ip)
        {

            string fileName = "START-SERVICES.bat";
            string pathCopyFrom = @"\\localhost\C$\hydra\HydraLive\trunk\Scripts";//source path
            string pathCopyTo = @"\\" + ip + @"\C$\" + forcePath;//target path
            string destFile = System.IO.Path.Combine(pathCopyTo, fileName);

            // Create a new target folder, if necessary.
            if (!System.IO.Directory.Exists(pathCopyTo))
            {
                System.IO.Directory.CreateDirectory(pathCopyTo);
            }

            //System.IO.File.Copy(pathCopyFrom, pathCopyTo, true);//copy the forcefiles folder from this machine to desired server

            if (System.IO.Directory.Exists(pathCopyFrom))
            {
                string[] files = System.IO.Directory.GetFiles(pathCopyFrom);

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    if (s.Contains(fileName))
                    {
                        destFile = System.IO.Path.Combine(pathCopyTo, fileName);
                        System.IO.File.Copy(s, destFile, true);
                    }
                }
            }
            else
            {
                Trace.WriteLine("Source path does not exist!");
            }

            return "";
        }

        //Edit/save text file for pattern desired, to a file
        //Then overwrite FO.txt that resides on server
        public static string EditFOtxtOnHydra(List<string> pattern, string ip)
        {
            //Write an array of strings to a file.
            //Create a string array that consists of three lines.
            //string[] pattern = { "9", "17", "38", "51", "62", "11", "16", "37", "57", "69" };
            // WriteAllLines creates a file, writes a collection of strings to the file,
            // and then closes the file.  You do NOT need to call Flush() or Close().
            //Copy batch script to remote server

            System.IO.File.WriteAllLines(@"C:\hydra\HydraLive\trunk\Scripts\FO-"+ip+".txt", pattern);
            string copyFrom = scriptPath + @"FO-"+ip+".txt";
            string copyTo = @"\\" + ip + @"\C$\MTS\Plugh\FO.txt";
            System.IO.File.Copy(copyFrom, copyTo, true);

            return "";
        }
        //Find all users Active and Disc on the server
        public static string ImpactedUsers(string ip)
        {
            List<string> UserList = new List<string>();

            //Run Script to Query Users
            MorpheusController.CreateBatchFile("C:\\PSTools\\PSExec.exe \\\\%1 query user /server:%1", scriptPath + @"GetUsers.bat");
            string cmdOutput = MorpheusController.RunBatchFile(scriptPath + @"GetUsers.bat", ip);
            string[] lines = cmdOutput.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            string usersFormatted = "";

            //Go through each line and get the username line 
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Active") || lines[i].Contains("Disc"))
                {
                    string[] activeUser = lines[i].Split(null);
                    usersFormatted+=activeUser[1]+",";
                }
            }

            //If there are users, format it name,name,name.
            if(usersFormatted.Length > 0)
            {
                return (usersFormatted.Remove(usersFormatted.Length - 1, 1) + ".");
            }
            //else return none if there are no users.
            else
            {
                return "None.";
            }                       
        }
        public static bool KillALLFO(string ip)
        {
            //Create script to kill notepads
            MorpheusController.CreateBatchFile("taskkill /s %1 /im \"notepad.exe\" /f", scriptPath + @"KillAllFO.bat");

            //Run it
            MorpheusController.RunBatchFile(scriptPath + @"KillAllFO.bat", ip);
            return true;
        }
        //Stop force - Disable-FOLGS
        public static string StopForce(string ip)
        {            
            KillALLFO(ip); //kill notepads
            MorpheusController.CreateBatchFile("C:\\PSTools\\psexec.exe \\\\" + ip + " -s -i \"C:\\"+forcePath+"Disable-FOLGS.bat", scriptPath + @"FORCE-OFF.bat");
            MorpheusController.RunBatchFile(scriptPath + @"FORCE-OFF.bat", ip);

            return "";
        }

        public static string AppendToHostsFileOnServer(string ip)
        {
            //Create batch file to update hosts file
            MorpheusController.CreateBatchFile("find /c \"Secure-Service\" C:\\windows\\system32\\drivers\\etc\\hosts || (echo. >> C:\\windows\\system32\\drivers\\etc\\hosts && echo 10.0.32.32 Secure-Service >> C:\\windows\\system32\\drivers\\etc\\hosts)", scriptPath + @"hostsFileAppend.bat");

            //Create batch file to run hosts file on server machine
            MorpheusController.CreateBatchFile("C:\\PSTools\\psexec.exe \\\\%1 C:\\"+forcePath+"hostsFileAppend.bat", scriptPath + @"Run-hostsFileAppend.bat");

            //Copy batch script to remote server
            string copyFrom = scriptPath + @"hostsFileAppend.bat";
            string copyTo = @"\\" + ip + @"\C$\" + forcePath + "hostsFileAppend.bat";
            System.IO.File.Copy(copyFrom, copyTo, true);

            //Run batch script on remote server
            BatchWithEcho(ip, @"Run-hostsFileAppend.bat", "");
            return "";
        }

        public static string EchoIntoFO(List<string> pattern, string ip)
        {
            MorpheusController.CreateBatchFile("C:\\PSTools\\psexec.exe \\\\%1 cmd /c \"echo %2 > C:\\MTS\\Plugh\\FO.txt\"", scriptPath + @"FORCE-ON-create.bat");
            MorpheusController.CreateBatchFile("C:\\PSTools\\psexec.exe \\\\%1 cmd /c \"echo %2 >> C:\\MTS\\Plugh\\FO.txt\"", scriptPath + @"FORCE-ON-append.bat");

            if (pattern.Count > 0)
            {
                BatchWithEcho(ip, @"FORCE-ON-create.bat", pattern[0]);
            }
            if (pattern.Count > 1)
            {
                for (int i = 1; i < pattern.Count; i++)
                {
                    BatchWithEcho(ip, @"FORCE-ON-append.bat", pattern[i]);
                }
            }
            return "";
        }
        public static string BatchWithEcho(string ip, string scriptName, string ball)
        {
            Process pr = new Process();
            pr.StartInfo.UseShellExecute = false;
            pr.StartInfo.RedirectStandardOutput = false;
            pr.StartInfo.RedirectStandardError = false;
            pr.StartInfo.FileName = scriptPath + scriptName;
            pr.StartInfo.Arguments = ip + " " + ball;
            pr.Start();
            pr.WaitForExit();
            return "";
        }

        //TODO:Get the players bingo card - taken from user within GUI
        public static string GetBingoCard()
        {
            return "";
        }

        //Check if the force is on or off on a server and return result
        public static string IsForceOn(string ip)
        {
            bool result = false;
            string text = "";
            string pathToCheck = @"\\" + ip + @"\C$\MTS\Plugh";//target path
            //check if plugh exists on the server
            if (System.IO.Directory.Exists(pathToCheck))
            {
                result = true;
            }

            if (result)
            {
                text = "ON";
            }
            else
            {
                text = "OFF";
            }

            return text;
        }
    }
}