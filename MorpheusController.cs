using HydraMVC.DAL;
using HydraMVC.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

/*   Log4Net Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
 
       http://www.apache.org/licenses/LICENSE-2.0
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

//Purpose of this class: Collect data from SQA servers and update in Servers table - SIH
namespace HydraMVC.Controllers
{
    public enum AutomationThreads { SERVER_STATUS, ACTIVE_USERS, SERVER_NAME, REPORT_SERVER, BOI_VERSION, HOTFIX, PLAYER_VERSION, GAMESERVER};

    public class MorpheusController : Controller
    {
        //Add logger declaration in classes for which we want to make logs.
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        public ActionResult Index()
        {
            return View();
            
        }
        public static string scriptPath = @"C:\hydra\HydraLive\trunk\Scripts\";
        
        //Harness threads for updating database, behind the main thread that the UI uses
        public Task PostAsyncAll(AutomationThreads automationThreads)
        {
            return Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    switch (automationThreads)
                    {
                        //Server Status Thread
                        case AutomationThreads.SERVER_STATUS:
                        {
                            UpdateAllServerStatus();
                            break;
                        }
                        //Active Users Thread
                        case AutomationThreads.ACTIVE_USERS:
                        {
                            UpdateAllActiveUsers();
                            break;
                        }
                        //Server Name Thread
                        case AutomationThreads.SERVER_NAME:
                        {
                            UpdateAllServerNames();
                            break;
                        }
                        //Report Server Name Thread
                        case AutomationThreads.REPORT_SERVER:
                        {
                            GenericRegistryUpdate("\\HKLM\\Software\\Cadillac Jack", "RSDB", "ReportServer", "reportServer.bat");
                            break;
                        }
                        //BOI version Thread
                        case AutomationThreads.BOI_VERSION:
                        {
                            GenericRegistryUpdate("\\HKLM\\SYSTEM\\Setup", "CJ-Image-Build", "BOI", "boi.bat");
                            break;
                        }
                        //HotFix Thread
                        case AutomationThreads.HOTFIX:
                        {
                            HotFixRegistryUpdate();
                            break;
                        }
                        //Player version Thread
                        case AutomationThreads.PLAYER_VERSION:
                        {
                            GetPlayerVersions();
                            break;
                        }
                        //Gameserver Version Thread
                        case AutomationThreads.GAMESERVER:
                        {
                            GameServerRegistryUpdate();
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }                
            });
        }
            
        public static bool SetServerStatus(string ip)
        {
            string status = "";
            bool serverIsOnline = false;
            string updateQuery = @"UPDATE [Servers] SET Status = @status WHERE IpAddress = @ip";

            long totalTime = 0;
            int timeout = 120;
            Ping pingSender = new Ping();
            //VERY quick method of pinging a host
            try
            {
                PingReply reply = pingSender.Send(ip, timeout);
                if (reply.Status == IPStatus.Success)
                {
                    serverIsOnline = true;
                    status = "ONLINE";
                    totalTime += reply.RoundtripTime;
                    Trace.WriteLine("PING SUCCESFUL on ip " + ip + " Time taken: " + reply.RoundtripTime);
                }
                else
                {
                    serverIsOnline = false;
                    status = "OFFLINE";
                }
            }
            catch
            {
                Trace.WriteLine("Ping FAILED on ip " + ip);
                status = "INVALID IP";
            }
            //Update the DB
            try
            {
                ServerContext db = new ServerContext();//instantiates a database context object
                db.Database.ExecuteSqlCommand(
                    updateQuery,
                    new SqlParameter("@status", status),
                    new SqlParameter("@ip", ip));
            }
            catch
            {
                //Output to the console
                Trace.WriteLine("ERROR: IP DOESNT EXIST IN SERVER TABLE - or some other issue is at hand!");
                
            }

            return serverIsOnline;
        }
        
        //AUTHOR:SIH
        public static void UpdateAllServerStatus()
        {       
            using (var context = new ServerContext())
            {
                var results = context.Database.SqlQuery<string>("SELECT IpAddress FROM Servers").ToArray();
                foreach (string ip in results)
                {                    
                    Trace.WriteLine("IpAddress:" + ip);
                    Trace.WriteLine("Calling SetServerStatus, sending in ip: " + ip);
                    SetServerStatus(ip);
                }
            }
        }
        public static bool CreateBatchFile(string content, string file)
        {
            //Create registry file
            try
            {
                string cmdCommand = content;
                System.IO.StreamWriter SW = new System.IO.StreamWriter(file);
                SW.WriteLine(@cmdCommand);//Add @ sign in front of string to prevent errors caused by unescaped backslash characters
                SW.Flush();
                SW.Close();
                SW.Dispose();
                SW = null;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static string RunBatchFile(string file, string arguements)
        {
            //System.IO.StreamWriter SW = new System.IO.StreamWriter(scriptPath + @"ScriptErrors.txt",true);
            // Start the child process.
            Process pr = new Process();
            // Redirect the output stream of the child process
            pr.StartInfo.UseShellExecute = false;
            pr.StartInfo.RedirectStandardOutput = true;
            pr.StartInfo.RedirectStandardError = true;
            pr.StartInfo.FileName = file;             
            pr.StartInfo.Arguments = arguements;
            pr.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // Read the output stream first and then wait.
            string cmdOutput = pr.StandardOutput.ReadToEnd();
            string cmdError = pr.StandardError.ReadToEnd();
            //SW.WriteLine(cmdOutput);//Add @ sign in front of string to prevent errors caused by unescaped backslash characters
            //SW.WriteLine(cmdError);//Add @ sign in front of string to prevent errors caused by unescaped backslash characters
            pr.WaitForExit();
                        
            //SW.Flush();
            //SW.Close();
            //SW.Dispose();
            //SW = null;
            return cmdOutput;
        }
        public static void HotFixRegistryUpdate()
        {

            string cmdOutput = "";
            //Location of HFs in registry
            CreateBatchFile("reg query \\\\%1\\HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall /s /f \"_HF\" /reg:64", scriptPath + @"hotfix.bat");

            //For each server
            using (var context = new ServerContext())
            {
                var results = context.Database.SqlQuery<string>("SELECT IpAddress FROM Servers").ToArray();
                foreach (string ip in results)
                {
                    //Run batch script
                    cmdOutput = RunBatchFile(scriptPath + @"hotfix.bat", ip);                    
                    string[] lines = cmdOutput.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    string[] values;
                    string allHFs = "";
                    foreach(string line in lines)
                    {
                        values = line.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                        if (values.Length > 0)
                        {
                            //Look for DisplayName
                            if(values[0] == "DisplayName")
                            {
                                allHFs = allHFs += values[values.Length - 1] + ",";                                
                            }
                        }
                    }
                    //Update the DB
                    if (allHFs.Length > 0)
                    {
                        try
                        {
                            UpdateDBField(ip, "HotFixes", allHFs.Substring(0, allHFs.Length - 1));
                        }
                        catch
                        {
                            Console.WriteLine("Error entering into DB");
                        }
                    }
                    
                }
            }
        }

        public static void HotFixRegistryUpdateOneIp(string ip)
        {
            string cmdOutput = "";
            //Location of HFs in registry
            CreateBatchFile("reg query \\\\%1\\HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall /s /f \"_HF\" /reg:64", scriptPath + @"hotfix.bat");

            //Run batch script
            cmdOutput = RunBatchFile(scriptPath + @"hotfix.bat", ip);
            string[] lines = cmdOutput.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            string[] values;
            string allHFs = "";
            foreach (string line in lines)
            {
                values = line.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                if (values.Length > 0)
                {
                    //Look for DisplayName
                    if (values[0] == "DisplayName")
                    {
                        allHFs = allHFs += values[values.Length - 1] + ",";
                    }
                }
            }
            //Update the DB
            if (allHFs.Length > 0)
            {
                try
                {
                    UpdateDBField(ip, "HotFixes", allHFs.Substring(0, allHFs.Length - 1));
                }
                catch
                {
                    Console.WriteLine("Error entering into DB");
                }
            }
        }

        public static void UpdateDBField(string ip, string field, string value)
        {
            string updateQuery = @"UPDATE [Servers] SET "+field+" = @value WHERE IpAddress = @ip";
            MorpheusController m = new MorpheusController();//so we can add log entries

            using (var context = new ServerContext())
            {
                string currentValue = context.Database.SqlQuery<string>("Select "+field+" from Servers where IpAddress="+"'"+ip+"'").FirstOrDefault<string>();
                Console.WriteLine(currentValue);
                if (currentValue == value)
                {
                    return;
                }
            }
            try
            {
                ServerContext db = new ServerContext();//instantiates a database context object
                db.Database.ExecuteSqlCommand(
                    updateQuery,
                    new SqlParameter("@value", value),
                    new SqlParameter("@ip", ip));
            }
            catch
            {
                //Output to the console
                Trace.WriteLine("ERROR: Server is offline: cannot retrieve active users for ip " + ip + " at this time.");
                m.logger.Error("ERROR: Server is offline: cannot retrieve active users for ip " + ip + " at this time.");
            }
        }
        public static void GenericRegistryUpdate(string location, string value, string field, string filename)
        {
            //Update DB from read registry value
            //%1-Location %2-Value %3-x64 vs x32
            CreateBatchFile("reg query %1 /v %2 %3",scriptPath + filename);
            using (var context = new ServerContext())
            {
                var results = context.Database.SqlQuery<string>("SELECT IpAddress FROM Servers").ToArray();
                foreach (string ip in results)
                {
                    string registry = GetRegistryValue_Generic(ip+location, value, filename);
                    string[] cmdSplit = registry.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                    string regValue = cmdSplit[cmdSplit.Length - 1];
                    if (!regValue.Contains("reg"))
                    {
                        UpdateDBField(ip, field, regValue);
                    }
                }
            }
        }

        public static void GenericRegistryUpdateOneIp(string ip, string location, string value, string field, string filename)
        {
            CreateBatchFile("reg query %1 /v %2 %3",scriptPath + filename);
            string registry = GetRegistryValue_Generic(ip+location, value, filename);
            string[] cmdSplit = registry.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
            string regValue = cmdSplit[cmdSplit.Length - 1];
            if (!regValue.Contains("reg"))
            {
                UpdateDBField(ip, field, regValue);
            }
        }

        //Get a value from the registry
        public static string GetRegistryValue_Generic(string location, string value, string filename)
        {
            string registryValue = RunBatchFile(scriptPath + filename, "\"\\\\" + location + "\" " + value + " " + "/reg:64");
            Trace.WriteLine("REGISTRY VALUE = " + registryValue);
            return registryValue;
        }

        //Run cmd command query user /server:<ip>
        //Save result to a variable
        //Parse the string and grab names of active users
        public static string GetActiveUsers(string ip)
        {
            string cmdCommand = "query user /server:" + ip;
            string cmdOutput = "";
            string activeUsers = "";
            string updateQuery = @"UPDATE [Servers] SET ActiveUsers = @activeUsers WHERE IpAddress = @ip";
            List<string> activeUserList = new List<string>();
            MorpheusController m = new MorpheusController();//so we can add log entries

            //write and run .bat file.
            //System.IO.StreamWriter SW = new System.IO.StreamWriter("C:\\Users\\SHealy\\Documents\\Visual Studio 2013\\Projects\\Hydra-6-withEmail\\trunk\\QueryUsers.bat");
            System.IO.StreamWriter SW = new System.IO.StreamWriter(scriptPath + @"QueryUsers.bat");
            SW.WriteLine(@cmdCommand);//Add @ sign in front of string to prevent errors caused by unescaped backslash characters
            SW.Flush();
            SW.Close();
            SW.Dispose();
            SW = null;
            

            // Start the child process.
            Process pr = new Process();
            // Redirect the output stream of the child process
            pr.StartInfo.UseShellExecute = false;
            pr.StartInfo.RedirectStandardOutput = true;
            //pr.StartInfo.CreateNoWindow = true;
            pr.StartInfo.FileName = scriptPath+"QueryUsers.bat";
            pr.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // Read the output stream first and then wait.
            cmdOutput = pr.StandardOutput.ReadToEnd();
            pr.WaitForExit();

            Trace.WriteLine("Users Query result: " + cmdOutput);

            //Parse the output for all active users
            //ParseActiveUsers will return a list of active user names
            activeUserList.AddRange(ParseActiveUsers(cmdOutput));

            //Add each active user name to a string
            if(activeUserList.Count > 1)//if more than 1 name exists in active users list
            {
                foreach (string value in activeUserList)
                {
                    activeUsers += value +  ",";
                }
                activeUsers = activeUsers.TrimEnd(',');//get rid of the last comma
            }
            else if (activeUserList.Count == 1)
            {
                foreach (string value in activeUserList)
                {
                    activeUsers += value;
                }
            }
            else
            {
                //do nothing, because there are no active users on the server
            }

            //Update the DB with active user names
            try
            {
                ServerContext db = new ServerContext();//instantiates a database context object
                db.Database.ExecuteSqlCommand(
                    updateQuery,
                    new SqlParameter("@activeUsers", activeUsers),
                    new SqlParameter("@ip", ip));
            }
            catch
            {
                //Output to the console
                Trace.WriteLine("ERROR: Server is offline: cannot retrieve active users for ip " + ip + " at this time.");
                m.logger.Error("ERROR: Server is offline: cannot retrieve active users for ip " + ip + " at this time.");
            }

            return activeUsers;
        }

        public static List<string> ParseActiveUsers(string cmdOutput)
        {
            List<string> activeUserList = new List<string>();
            List<string> names = new List<string>();
            List<string> lines2 = new List<string>();
            string[] activeUserArray;
            bool isActive = false;

            //Put each line in the string into array indexes
            string[] lines = cmdOutput.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            //Remove elements 0, 1, 2 and the last element (uneeded)
            //Arrays are immutable, so actually create a new array with only the items needed - (3 through array length - 2)
            for (int i = 3; i < lines.Length - 1; i++)
            {
                lines2.Add(lines[i]);
                activeUserArray = lines[i].Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                Trace.WriteLine("activeUserArray 0 =" + activeUserArray[0]);

                //Add username to 'names' list ONLY if the 'Active' string exists in 'activeUserList'
                foreach (string value in activeUserArray)
                {
                    if (value == "Active")
                    {
                        isActive = true;
                        //Trace.WriteLine("This user is currently Active");
                        names.Add(activeUserArray[0]);
                    }
                }
            }

            Trace.WriteLine("Contents of names list below:");
            foreach (string value in names)
            {
                Trace.WriteLine(value);
            }
                
            return names;
        }

        //Update Servers table with current active users
        public static void UpdateAllActiveUsers()
        {
            using (var context = new ServerContext())
            {
                var results = context.Database.SqlQuery<string>("SELECT IpAddress FROM Servers").ToArray();
                foreach (string ip in results)
                {
                    //Trace.WriteLine("IpAddress:" + ip);
                    Trace.WriteLine("Calling GetActiveUsers(), sending in ip: " + ip);
                    GetActiveUsers(ip);
                }
            }
        }

        public static void GetPlayerVersions()
        {
            string playerFolderName;
            string playerVersion;
            string playerRevision;
            string playerVersionRevision;
            string playerVersionFinal = "";
            string pathCopyTo = scriptPath + @"cjplayer_CVD.xml";
            MorpheusController m = new MorpheusController();//so we can add log entries

            using (var context = new ServerContext())
            {
                var results = context.Database.SqlQuery<string>("SELECT IpAddress FROM Servers").ToArray();

                foreach (string ip in results)
                {
                    try
                    {
                        //Declare the lists here so they clear out each time
                        List<string> playerFolderNameList = new List<string>();
                        List<string> playerVersionsList = new List<string>();
                        string subFolderPath = @"\\" + ip + @"\C$\MTS\Player\CJ\CJOS64\3.3\";
                        //Get an array of directory names, within the 3.3 directory
                        //TODO:check if all servers have a 3.3 directory, if this can be different, then handle that case
                        foreach (string s in System.IO.Directory.GetDirectories(subFolderPath))
                        {
                            playerFolderNameList.Add(s.Remove(0, subFolderPath.Length));
                        }

                        foreach (string s in playerFolderNameList)
                        {
                            playerFolderName = s;//get the current player folder name
                            string pathCopyFrom = @"\\" + ip + @"\C$\MTS\Player\CJ\CJOS64\3.3\" + playerFolderName + @"\CVD\cjplayer_CVD.xml";//set path to copy from
                            System.IO.File.Copy(pathCopyFrom, pathCopyTo, true);//copy the player xml file from server to this machine

                            //Open xml file and save contents to a variable
                            string readText = System.IO.File.ReadAllText(pathCopyTo);
                            Trace.WriteLine("Contents of cjplayer_CVD.xml: " + readText);

                            //Grab the player version
                            playerVersion = GetBetween(readText, "<VERSION>V", "</VERSION>");
                            Trace.WriteLine("Contents of playerVersion: " + playerVersion);

                            //Grab the player revision
                            playerRevision = GetBetween(readText, "<REVISION>", "</REVISION>");
                            Trace.WriteLine("Contents of playerRevision: " + playerRevision);

                            //Build the string that will be saved to a list of playerversions
                            playerVersionRevision = playerVersion + ":" + playerRevision;
                            Trace.WriteLine("Contents of playerVersion with Revision added, on server at ip: " + ip + " -> " + playerVersionRevision);

                            //Add playerVersionRevision to a list
                            playerVersionsList.Add(playerVersionRevision);
                        }

                        //Build a comma delimited string of player versions
                        if (playerVersionsList.Count > 1)//if more than 1 player version exists in player versions list
                        {
                            foreach (string value in playerVersionsList)
                            {
                                playerVersionFinal += value + ",  ";
                            }
                            playerVersionFinal = playerVersionFinal.TrimEnd(' ');//get rid of extra spaces
                            playerVersionFinal = playerVersionFinal.TrimEnd(',');//get rid of the last comma
                        }
                        else if (playerVersionsList.Count == 1)
                        {
                            foreach (string value in playerVersionsList)
                            {
                                playerVersionFinal += value;
                            }
                        }
                        else
                        {
                            Trace.WriteLine("No players exist on ip " + ip);
                        }

                        //Save to database
                        Trace.WriteLine("Saving playerVersionFinal to database...");
                        UpdateDBField(ip, "PlayerVersions", playerVersionFinal);
                        playerVersionFinal = "";//clear variable contents
                    }
                    catch
                    {
                        Trace.WriteLine("BAM! There was a problem with locating the xml file on ip " + ip);
                        m.logger.Error("BAM! There was a problem with locating the xml file on ip " + ip);
                    }
                }
            }
        }

        public static void GetPlayerVersionsOneIp(string ip)
        {
            string playerFolderName;
            string playerVersion;
            string playerRevision;
            string playerVersionRevision;
            string playerVersionFinal = "";
            string pathCopyTo = scriptPath + @"cjplayer_CVD.xml";
            MorpheusController m = new MorpheusController();//so we can add log entries

            try
            {
                //Declare the lists here so they clear out each time
                List<string> playerFolderNameList = new List<string>();
                List<string> playerVersionsList = new List<string>();
                string subFolderPath = @"\\" + ip + @"\C$\MTS\Player\CJ\CJOS64\3.3\";
                //Get an array of directory names, within the 3.3 directory
                //TODO:check if all servers have a 3.3 directory, if this can be different, then handle that case
                foreach (string s in System.IO.Directory.GetDirectories(subFolderPath))
                {
                    playerFolderNameList.Add(s.Remove(0, subFolderPath.Length));
                }

                foreach (string s in playerFolderNameList)
                {
                    playerFolderName = s;//get the current player folder name
                    string pathCopyFrom = @"\\" + ip + @"\C$\MTS\Player\CJ\CJOS64\3.3\" + playerFolderName + @"\CVD\cjplayer_CVD.xml";//set path to copy from
                    System.IO.File.Copy(pathCopyFrom, pathCopyTo, true);//copy the player xml file from server to this machine

                    //Open xml file and save contents to a variable
                    string readText = System.IO.File.ReadAllText(pathCopyTo);
                    Trace.WriteLine("Contents of cjplayer_CVD.xml: " + readText);

                    //Grab the player version
                    playerVersion = GetBetween(readText, "<VERSION>V", "</VERSION>");
                    Trace.WriteLine("Contents of playerVersion: " + playerVersion);

                    //Grab the player revision
                    playerRevision = GetBetween(readText, "<REVISION>", "</REVISION>");
                    Trace.WriteLine("Contents of playerRevision: " + playerRevision);

                    //Build the string that will be saved to a list of playerversions
                    playerVersionRevision = playerVersion + ":" + playerRevision;
                    Trace.WriteLine("Contents of playerVersion with Revision added, on server at ip: " + ip + " -> " + playerVersionRevision);

                    //Add playerVersionRevision to a list
                    playerVersionsList.Add(playerVersionRevision);
                }

                //Build a comma delimited string of player versions
                if (playerVersionsList.Count > 1)//if more than 1 player version exists in player versions list
                {
                    foreach (string value in playerVersionsList)
                    {
                        playerVersionFinal += value + ",  ";
                        //15.3.11:98084 ,15.3.15:108209 ,15.4:110093 ,15.4.1:113486

                    }
                    playerVersionFinal = playerVersionFinal.TrimEnd(' ');//get rid of extra spaces
                    playerVersionFinal = playerVersionFinal.TrimEnd(',');//get rid of the last comma
                }
                else if (playerVersionsList.Count == 1)
                {
                    foreach (string value in playerVersionsList)
                    {
                        playerVersionFinal += value;
                    }
                }
                else
                {
                    Trace.WriteLine("No players exist on ip " + ip);
                }

                //Save to database
                Trace.WriteLine("Saving playerVersionFinal to database...");
                UpdateDBField(ip, "PlayerVersions", playerVersionFinal);
                playerVersionFinal = "";//clear variable contents
            }
            catch
            {
                Trace.WriteLine("BAM! There was a problem with locating the xml file on ip " + ip);
                m.logger.Error("BAM! There was a problem with locating the xml file on ip " + ip);
            }

        }
        
        //Return text that's in between 2 strings
        public static string GetBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }

        public static void GameServerRegistryUpdate()
        {
            string cmdOutput = "";
            //Creation of batch scripts
            CreateBatchFile("reg query \\\\%1\\HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall /s /f \"Gameserver\" /reg:64", scriptPath + @"gameserver.bat");
            CreateBatchFile("reg query %1 /v %2 %3", scriptPath + @"gameserverupdate.bat");

            using (var context = new ServerContext())
            {
                var results = context.Database.SqlQuery<string>("SELECT IpAddress FROM Servers").ToArray();
                //For each IP
                foreach (string ip in results)
                {
                    //Run batch script
                    cmdOutput = RunBatchFile(scriptPath + @"gameserver.bat", ip);
                    string[] lines = cmdOutput.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                    //For each line in the batch return
                    for (int i = 0; i < lines.Length; i++)
                    {
                        //Confirm it is the "gameserver", not the "linked gameserver"
                        if (lines[i].ToLower().Contains("gameserver") && lines[i].Contains("DisplayName") && !lines[i].ToLower().Contains("linked"))
                        {
                            for (int k = i; k >= 0; k--)
                            {
                                //Get the registry directory location for the game server
                                if (lines[k].Contains("{") && lines[k].Contains("}"))
                                {
                                    //Get the DisplayVersion of the gameserver from the registry
                                    string foundLine = lines[k];
                                    string registry = GetRegistryValue_Generic(ip + @"\" + foundLine, "DisplayVersion", @"gameserverupdate.bat");
                                    string[] cmdSplit = registry.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                                    string regValue = cmdSplit[cmdSplit.Length - 1];
                                    if (!regValue.Contains("reg"))
                                    {
                                        //Update the DB
                                        UpdateDBField(ip, "GameServer", regValue);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void GameServerRegistryUpdateOneIp(string ip)
        {
            string cmdOutput = "";
            //Creation of batch scripts
            CreateBatchFile("reg query \\\\%1\\HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall /s /f \"Gameserver\" /reg:64", scriptPath + @"gameserver.bat");
            CreateBatchFile("reg query %1 /v %2 %3", scriptPath + @"gameserverupdate.bat");

            //Run batch script
            cmdOutput = RunBatchFile(scriptPath + @"gameserver.bat", ip);
            string[] lines = cmdOutput.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            //For each line in the batch return
            for (int i = 0; i < lines.Length; i++)
            {
                //Confirm it is the "gameserver", not the "linked gameserver"
                if (lines[i].ToLower().Contains("gameserver") && lines[i].Contains("DisplayName") && !lines[i].ToLower().Contains("linked"))
                {
                    for (int k = i; k >= 0; k--)
                    {
                        //Get the registry directory location for the game server
                        if (lines[k].Contains("{") && lines[k].Contains("}"))
                        {
                            //Get the DisplayVersion of the gameserver from the registry
                            string foundLine = lines[k];
                            string registry = GetRegistryValue_Generic(ip + @"\" + foundLine, "DisplayVersion", @"gameserverupdate.bat");
                            string[] cmdSplit = registry.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                            string regValue = cmdSplit[cmdSplit.Length - 1];
                            if (!regValue.Contains("reg"))
                            {
                                //Update the DB
                                UpdateDBField(ip, "GameServer", regValue);
                            }
                        }
                    }
                }
            }
        }

        public static void ServerNameUpdate(string ip)
        {
            string regResult = "";
            bool containsQAGS = false;
            //Look for the proper server name value, determine if its QARS or QAGS.
            regResult = GetRegistryValue_Generic(ip + @"\HKLM\Software\Cadillac Jack", "ComputerName", @"serverName.bat");

            //Search regResult for QAGS
            containsQAGS = regResult.Contains("QAGS");

            Trace.WriteLine("regResult for ip: " + ip + " = " + regResult);
            Trace.WriteLine("Does regResult contain the string QAGS? -> " + containsQAGS);

            //QARS server registries do not contain 'QAGS', so update the database accordingly.
            if (containsQAGS)//then this is a game server
            {
                GetRegistryValue_Generic(ip + @"\HKLM\Software\Cadillac Jack", "ComputerName", @"serverName.bat");
                //parse the string, extract the game server name, then update db field.
                string[] cmdSplit = regResult.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                string regValue = cmdSplit[cmdSplit.Length - 1];

                if (!regValue.Contains("reg"))
                {
                    Trace.WriteLine("regValue = " + regValue);
                    UpdateDBField(ip, "ServerName", regValue);
                }
            }
            else//this is a report server
            {
                regResult = GetRegistryValue_Generic(ip + @"\HKLM\Software\Cadillac Jack", "RSDB", @"serverName.bat");
                //parse the string, extract the game server name, then update db field.
                string[] cmdSplit = regResult.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                string regValue = cmdSplit[cmdSplit.Length - 1];

                if (!regValue.Contains("reg"))
                {
                    Trace.WriteLine("regValue = " + regValue);
                    UpdateDBField(ip, "ServerName", regValue);
                }
            }
        }

        //Update Servers table with current active users
        public static void UpdateAllServerNames()
        {
            
            using (var context = new ServerContext())
            {
                var results = context.Database.SqlQuery<string>("SELECT IpAddress FROM Servers").ToArray();
                foreach (string ip in results)
                {
                    //Trace.WriteLine("IpAddress:" + ip);
                    Trace.WriteLine("Calling ServerNameUpdate(), sending in ip: " + ip);
                    ServerNameUpdate(ip);
                }
            }
        }

        //SIH
        //Call this method when the create page submit button is clicked, and send in the ip the user entered.
        //Additionally, call this method in the GUI thread so the fields are updated before GUI loads.
        public static void UpdateAllFieldsOnCreate(string ip)
        {
            Trace.WriteLine("Calling UpdateAllFieldsOnCreate()");
            SetServerStatus(ip);
            GetActiveUsers(ip);
            ServerNameUpdate(ip);
            GenericRegistryUpdateOneIp(ip, "\\HKLM\\Software\\Cadillac Jack", "RSDB", "ReportServer", "reportServer.bat");
            GenericRegistryUpdateOneIp(ip, "\\HKLM\\SYSTEM\\Setup", "CJ-Image-Build", "BOI", "boi.bat");
            HotFixRegistryUpdateOneIp(ip);
            GetPlayerVersionsOneIp(ip);
            GameServerRegistryUpdateOneIp(ip);
        }

        //Example of sending email from a view:
        //@HydraMVC.Controllers.MorpheusController.SendEmail("TESTING 1,2,3", "AUTOMATED TEST EMAIL FROM HYDRA!!!", "shealy@playags.com");
        public static string SendEmail(string subject, string emailBody, string toMail)
        {
            MailMessage mailMessage = new MailMessage();
            MailAddress fromAddress = new MailAddress("S_Hydra@playags.com");
            mailMessage.From = fromAddress;
            mailMessage.To.Add(toMail);
            mailMessage.Body = emailBody;
            mailMessage.IsBodyHtml = true;
            mailMessage.Subject = subject;
            SmtpClient smtpClient = new SmtpClient();
            //smtpClient.Host = "exchange.playags.com";
            smtpClient.Host = "mail.atl.cadillacjack.com";
            smtpClient.Port = 25;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new System.Net.NetworkCredential("S_Hydra@playags.com",
            "8w(*FT5q");//Senders username and password
            smtpClient.Send(mailMessage);

            return "";
        }
        
        //Multisite(formerly known as WAP) state can be acquired from C/MTS/Services/EGSServices.xml
        public static string GetMultiSiteStateFromServer(string ip)
        {
            string pathCopyFrom = @"\\" + ip + @"\C$\MTS\Services\EGSServices.xml";//set path to copy from
            string pathCopyTo = scriptPath + @"EGSServices.xml";

            try
            {
                System.IO.File.Copy(pathCopyFrom, pathCopyTo, true);//copy the EGSServices file from server to this machine

                //Save contents of xml file to a string
                string readText = System.IO.File.ReadAllText(pathCopyTo);
                //check if "WAPServer" exists in the string
                if (readText.Contains("WAP"))
                {
                    return "Y -";
                }
                else
                {
                    return "N -";
                }
            }
            catch (Exception)
            {
                return "N";
            }
        }

        //We need to know what PID's have been assigned to WAP, because they can NOT be used for Local Area progressives.
        //This data can be found in the Bingo database, progressives table.
        //Progressive Type 102 is WAP (100 = local, 101 is Turboboost)
        //select Bingo.dbo.Progressives.GameOutcomeID from Bingo.dbo.Progressives where Bingo.dbo.Progressives.ProgressiveTypeLookupID = 102;
        public static string GetMultisitePIDsFromServer(string ip)
        {
            string pids = "";

            MorpheusController.CreateBatchFile(@"sqlcmd -S " + ip + @" -d Bingo -E -o " + scriptPath + @"WAPPIDresult.txt -Q ""SELECT GameOutcomeID FROM Progressives WHERE ProgressiveTypeLookupID = 102"" ", scriptPath + @"GetWAPPids.bat");

            //Run the batch file
            try
            {
                MorpheusController.RunBatchFile(scriptPath + @"GetWAPPids.bat", ip);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.StackTrace.ToString());
            }
            
            //Parse the cmd output and return pids, comma delimited
            //Read WAPPIDresult.txt into a variable
            string[] lines = System.IO.File.ReadAllLines(scriptPath + @"WAPPIDresult.txt");
            //Add desired indexes to the string
            int index = 0;
            foreach(string value in lines){
                if(index > 1 && index < lines.Length - 2){
                    //pidLis.Add(value);
                    //add to the string
                    pids += value + ", ";
                }
                index++;
            }
            //delete last comma
            if (!pids.Equals(""))
            {
                pids = pids.Remove(pids.LastIndexOf(','));
                return pids;
            }
            else
            {
                pids = " - ERROR connecting to this server: WAP PID query failed as a result.";
                return pids;
            }
        }
    }
}