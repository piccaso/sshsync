using System;
using System.Reflection;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using Toddsoft.SSH;
using ToddSoft.Tools.CommandLine;

/*
 *
 * SshSync
 * 
 * Description:-
 *   A tool to allow intelligent Secure FTP transmissions. SshSync only support pull type transfers, but it allows 
 *   use of a Private Key to ensure that authentication is secure. A text file that contains a list of files always 
 *   processed is used to check that only 'new' files are retrieved.
 * 
 * Features:-
 *   - SFTP Pull over SSH
 *   - Fully configurable input parameter, including port number
 *   - Password or public key authentication are the only authentication type allowed
 *   - Test mode enables diagnostics without actually pulling over any files
 *   - .Net 2.0 managed code allows easy integration into other projects
 *   - Catalog file used for history of previous file transfers
 *   - It's free!
 * 
 * Command Line Parameters:-
 *   Use the /? switch to retrieve a full list of input parameters
 *   e.g.  SshSync.exe /?
 * 
 * Error Codes:-
 * 
 * The following errors are thrown by SshSync. They will be printed to the console and written to the
 * Windows Application Event Log under the application name SshSync.
 * 
 *  *** CRITICAL ERRORS ***
 *  3000 - Error converting PortNumber to integer
 *  3001 - SSH Connection failed  (SSHSYNC_SSHCONNERR)
 *  3002 - Error while retrieving a specific file
 *  3003 - 'Unknown' error occurred during SSH activity
 *  3004 - Error writing to Destination folder
 *  3005 - Error accessing Catalog file
 *  3006 - SSH Connection timed out
 *  3007 - Error writing to the catalog file
 *  3008 - SSH Disconnect failed
 *  3009 - Invalid Parameters
 *  3010 - Invalid Catalog File
 *  3011 - Private Key and Password details invalid
 *  3099 - Unknown Error
 * 
 * 
 * Release History:-
 * 
 * DATE             Version   Assembly           Details
 * -------------------------------------------------------------------------------------
 * 20th Jun 2007     v1.0      1.0.0.0           Initial Release
 *  9th Jul 2007     v1.0a     1.0.2746.24953    Updated ToddSoft library, allow empty catalog file
 *                                               Add filesize check for retrieved file
 *                                               Update help details, add timeout parameter
 *                                               Add copyright notices for SharpSsh
 *  6th Nov 2007     v1.0d     1.0.0.0           Increase error handling and verbosity
 *                                               Remove file after unsuccessful file retrieval
 *                                               Fix Spelling mistakes!
 *  4th Jan 2008     v1.0e                       Add catalog cleanup feature
 * 16th May 2008     v1.0f                       Remove ToddSoft Libraries
 *  2nd Feb 2009     v1.0g                       Add extra error handling
 * 
 * 
 * Future
 *          2008     v1.1                        Add push file option
 * 
 * 
 *   ====================================================================
 *   ==         SshSync Copyright 2007 Colin Todd, ToddSoft            ==
 *   ====================================================================
 *   Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *   1. Redistributions of source code must retain the above copyright notice,
 *   this list of conditions and the following disclaimer.
 *  
 *   2. Redistributions in binary form must reproduce the above copyright 
 *   notice, this list of conditions and the following disclaimer in 
 *   the documentation and/or other materials provided with the distribution.
 *  
 *   3. The names of the authors may not be used to endorse or promote products
 *   derived from this software without specific prior written permission.
 *  
 *   THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
 *   INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 *   FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR
 *   OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
 *   INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 *   LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
 *   OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 *   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 *   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 *   EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 * 
 * 
 * 
 **/

namespace Toddsoft.Application
{
    public class CommandLine
    {
        public static void Main(String[] Args)
        {
            //Set Defaults
            int DefaultPortNumber = 22;
            int DefaultConnectionTimeout = 20000;
            int DefaultMaxCatalogFileRows = 10000;
            int MaxCatalogFileRows = DefaultMaxCatalogFileRows;
            String DefaultWildCard = @"*.*";
            Boolean bDebug = false;

            Arguments CommandLine;

            #region User Input Declaration
            String HostName = null;
            String UserName = null;
            String Password = null;
            String LocalCatalogFile = null;
            String FileWildCard = null;
            String DestinationFolder = null;
            int PortNumber = DefaultPortNumber;
            int ConnectionTimeout = DefaultConnectionTimeout;
            String PrivateKey = null;
            String SSHFolderPath = null;
            Boolean bTestConnection = false;
            Boolean bCommandLineArgsValid = true;

            String strUsageText = null;
            String strCopyRightText = null;
            #endregion


            strUsageText = SetUsage(DefaultPortNumber, DefaultWildCard, DefaultConnectionTimeout, DefaultMaxCatalogFileRows);
            strCopyRightText = SetCopyrightDetails();

            #region Parse Command Line Parameters
            // Command line parsing
            CommandLine = new Arguments(Args);

            #region HELP
            if (CommandLine.ParameterCount == 0
                || (CommandLine["?"] != null
                || CommandLine["help"] != null
                || CommandLine["h"] != null))
            {
                ToddSoft.Tools.Util.Debug(strUsageText, true);
                Environment.Exit(Toddsoft.SSH.SshSync.SSHSYNC_INVLDPARAMTRS);
            }
            #endregion

            #region VERSION
            if (CommandLine["V"] != null)
            {

                String Assembly_DotNetRunTime = Assembly.GetExecutingAssembly().ImageRuntimeVersion;
                String Assembly_Name = Assembly.GetExecutingAssembly().GetName().Name.ToString();
                String Assembly_Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                ToddSoft.Tools.Util.Debug("Name               : " + Assembly_Name, true);
                ToddSoft.Tools.Util.Debug("Assembly Version   : " + Assembly_Version, true);
                ToddSoft.Tools.Util.Debug("RunTime Version    : " + Assembly_DotNetRunTime, true);

                ToddSoft.Tools.Util.Debug(strCopyRightText, true);
                
                Environment.Exit(Toddsoft.SSH.SshSync.SSHSYNC_OK);
            }
            #endregion

            #region DEBUG
            if (CommandLine["debug"] != null)
            {
                bDebug = true;
                ToddSoft.Tools.Util.Debug("DebugMode : On", bDebug);
                ToddSoft.Tools.Util.Debug("ParameterCount : " + CommandLine.ParameterCount, bDebug);
            }
            #endregion

            #region SERVERNAME
            if (CommandLine["S"] != null && CommandLine["S"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("ServerName: " + CommandLine["S"], bDebug);
                HostName = CommandLine["S"];
            }
            else
            {
                // required argument not provided
                ToddSoft.Tools.Util.Debug("No parameter provided for SERVERNAME", true);
                bCommandLineArgsValid = false;
            }
            #endregion

            #region PORTNUMBER
            if (CommandLine["PN"] != null && CommandLine["PN"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("PortNumber: " + CommandLine["PN"], bDebug);
                try
                {
                    PortNumber = Convert.ToInt32(CommandLine["PN"]);
                }
                catch
                {
                    ToddSoft.Tools.Util.Debug("********* ERROR *********", bDebug);
                    ToddSoft.Tools.Util.Debug("ERROR: While converting PortNumber (" + CommandLine["PN"] + ") to integer", true);
                    ToddSoft.Tools.Util.LogEvent("SshSync", "Error converting Timeout parameter to integer", EventLogEntryType.Error, 3000);
                    ToddSoft.Tools.Util.Debug("********* ERROR *********", bDebug);
                    bCommandLineArgsValid = false;
                }
            }
            else
            {
                // required argument not provided
                ToddSoft.Tools.Util.Debug("PortNumber: " + DefaultPortNumber.ToString() + " [Default]", bDebug);
                PortNumber = DefaultPortNumber;
            }
            #endregion

            #region USERNAME
            if (CommandLine["U"] != null && CommandLine["U"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("UserName: " + CommandLine["U"], bDebug);
                UserName = CommandLine["U"];
            }
            else
            {
                // required argument not provided
                ToddSoft.Tools.Util.Debug("No parameter provided for USERNAME", true);
                bCommandLineArgsValid = false;
            }
            #endregion

            #region PASSWORD
            if (CommandLine["P"] != null)
            {
                ToddSoft.Tools.Util.Debug("Password: " + CommandLine["P"], bDebug);
                Password = CommandLine["P"];
            }
            else
            {
                Password = "";
            }
            #endregion

            #region PRIVATEKEY
            if (CommandLine["K"] != null && CommandLine["K"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("PrivateKey: " + CommandLine["K"], bDebug);
                PrivateKey = CommandLine["K"];

                if (!File.Exists(PrivateKey))
                {

                    ToddSoft.Tools.Util.Debug("Private Key file does not exist", true);
                    bCommandLineArgsValid = false;
                }
            }
            else
            {
                PrivateKey = "";
            }
            if ((PrivateKey == "") && (Password == ""))
            {
                // required argument not provided
                ToddSoft.Tools.Util.Debug("No parameter provided for PASSWORD or PRIVATEKEY", true);
                bCommandLineArgsValid = false;
            }
            #endregion

            #region SSHFOLDERPATH
            if (CommandLine["R"] != null && CommandLine["R"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("SshFolderPath: " + CommandLine["R"], bDebug);
                SSHFolderPath = CommandLine["R"];
            }
            else
            {
                // required argument not provided
                ToddSoft.Tools.Util.Debug("No parameter provided for SSHFOLDERPATH", true);
                bCommandLineArgsValid = false;
            }
            #endregion

            #region LOCALDESTINATIONPATH
            if (CommandLine["D"] != null && CommandLine["D"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("LocalDestinationPath: " + CommandLine["D"], bDebug);
                DestinationFolder = CommandLine["D"];
            }
            else
            {
                // required argument not provided
                ToddSoft.Tools.Util.Debug("No parameter provided for LOCALDESTINATIONPATH", true);
                bCommandLineArgsValid = false;
            }
            #endregion

            #region CATALOG FILE
            if (CommandLine["C"] != null && CommandLine["C"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("CatalogFile: " + CommandLine["C"], bDebug);
                LocalCatalogFile = CommandLine["C"];
            }
            else
            {
                ToddSoft.Tools.Util.Debug("CatalogFile: NONE", bDebug);
                LocalCatalogFile = "";
            }
            #endregion

            #region CATALOG FILE ROWS
            if (CommandLine["CR"] != null && CommandLine["CR"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("CatalogFileRows: " + CommandLine["CR"], bDebug);
                try
                {
                    MaxCatalogFileRows = Convert.ToInt32(CommandLine["CR"]);
                }
                catch
                {
                    ToddSoft.Tools.Util.Debug("********* ERROR *********", bDebug);
                    ToddSoft.Tools.Util.Debug("ERROR: While converting Catalog File Rows (" + CommandLine["CR"] + ") to integer", true);
                    ToddSoft.Tools.Util.LogEvent("SSHSync", "Error converting Catalog File Rows parameter to integer", EventLogEntryType.Error, 3000);
                    ToddSoft.Tools.Util.Debug("********* ERROR *********", bDebug);
                    bCommandLineArgsValid = false;
                }

            }
            else
            {
                ToddSoft.Tools.Util.Debug("CatalogFileRows: " + DefaultMaxCatalogFileRows + " (Default)", bDebug);
                MaxCatalogFileRows = DefaultMaxCatalogFileRows;
            }
            #endregion
            
            #region WILDCARD
            if (CommandLine["W"] != null && CommandLine["W"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("WildCard: " + CommandLine["W"], bDebug);
                FileWildCard = CommandLine["W"];
            }
            else
            {
                ToddSoft.Tools.Util.Debug("WildCard: " + DefaultWildCard + " [Default]", bDebug);
                FileWildCard = DefaultWildCard;
            }
            #endregion

            #region TIMEOUT
            if (CommandLine["T"] != null && CommandLine["T"].Length > 0)
            {
                ToddSoft.Tools.Util.Debug("Timeout: " + CommandLine["T"], bDebug);
                try
                {
                    ConnectionTimeout = Convert.ToInt32(CommandLine["T"]);
                }
                catch
                {
                    ToddSoft.Tools.Util.Debug("********* ERROR *********", bDebug);
                    ToddSoft.Tools.Util.Debug("ERROR: While converting Timeout (" + CommandLine["T"] + ") to integer", true);
                    ToddSoft.Tools.Util.LogEvent("SshSync", "Error converting Timeout parameter to integer", EventLogEntryType.Error, 3000);
                    ToddSoft.Tools.Util.Debug("********* ERROR *********", bDebug);
                    bCommandLineArgsValid = false;
                }
            }
            else
            {
                // required argument not provided
                ToddSoft.Tools.Util.Debug("Timeout: " + DefaultConnectionTimeout.ToString() + " [Default]", bDebug);
                ConnectionTimeout = DefaultConnectionTimeout;
            }
            #endregion

            #region TESTCONNECTION
            if (CommandLine["test"] != null)
            {
                ToddSoft.Tools.Util.Debug("TestConnection: " + CommandLine["test"], bDebug);
                bTestConnection = true;


                // This will test a SSH connection can be made
                // Authentication is successful
                // Directory listing can be retrieved
            }
            else
            {
                bTestConnection = false;
            }
            #endregion

            #region Check for Mandatory Parameters
            if (!bCommandLineArgsValid)
            {
                ToddSoft.Tools.Util.Debug("Manadatory Parameters not provided. Quitting", true);
                ToddSoft.Tools.Util.Debug("Use /? option for help", true);
                ToddSoft.Tools.Util.Debug("============================================", bDebug);
                Environment.Exit(Toddsoft.SSH.SshSync.SSHSYNC_INVLDPARAMTRS);
            }
            else
            {
                ToddSoft.Tools.Util.Debug("Command Line Parameters are Valid. Executing SshSync...", bDebug);
            }
            #endregion

#endregion

            #region Perform SshSync

            try
            {
                int returnCode = 0;
                ToddSoft.Tools.Util.Debug("============================================", bDebug);
                //Create SshSync Object with input parameters
                SshSync oSshSync = new SshSync(HostName, PortNumber, UserName, Password, PrivateKey, LocalCatalogFile, MaxCatalogFileRows, SSHFolderPath, FileWildCard, DestinationFolder, ConnectionTimeout, bDebug, bTestConnection);

                //Perform SSH Syncronization
                returnCode = oSshSync.PerformSSHSync();

                //Check return codes and return it to calling application
                if (returnCode != 0)
                {
                    ToddSoft.Tools.Util.Debug("============================================", bDebug);
                    Environment.Exit(returnCode);
                }

                //Close SSH Connection
                returnCode = oSshSync.Disconnect();

                //Check return codes and return it to calling application
                if (returnCode != 0)
                {
                    ToddSoft.Tools.Util.Debug("============================================", bDebug);
                    Environment.Exit(returnCode);
                }
            }
            catch (Exception e)
            {
                ToddSoft.Tools.Util.Debug("Error during execution", true);
                ToddSoft.Tools.Util.Debug(e.Message, true);
                ToddSoft.Tools.Util.Debug("============================================", bDebug);
                Environment.Exit(Toddsoft.SSH.SshSync.SSHSYNC_UNKNOWNERROR);
            }
            #endregion

            ToddSoft.Tools.Util.Debug("============================================", bDebug);
            Environment.Exit(Toddsoft.SSH.SshSync.SSHSYNC_OK);
        }

        public static String SetUsage(int DefaultPortNumber, String DefaultWildCard, int DefaultTimeout, int DefaultCatalogFileRows)
        {
            String strUsage;
            strUsage = @"Synchronize remote and local files via SSH.";
            strUsage += Environment.NewLine;

            strUsage += Environment.NewLine + @"Usage: SshSync /S:ServerName [/PN:PortNumber] /U:UserName /R:/RemoteSSHDir";
            strUsage += Environment.NewLine + @"                             /D:LocalDir [/P:Password | /K:PrivateKey] ";
            strUsage += Environment.NewLine + @"                             /C:CatalogFile [/W:WildCard] [/debug] [/test] [/?]";
            strUsage += Environment.NewLine + @"Options :";
            strUsage += Environment.NewLine + @"  /S:ServerName            ServerName running SSH Server";
            strUsage += Environment.NewLine + @"  /PN:PortNumber           Port Number that SSH is running on";
            strUsage += Environment.NewLine + @"                                  Default : " + DefaultPortNumber.ToString();
            strUsage += Environment.NewLine + @"  /U:UserName              Username";
            strUsage += Environment.NewLine + @"  /P:Password              Password for authentication";
            strUsage += Environment.NewLine + @"  /K:PrivateKey            Private key for authentication";
            strUsage += Environment.NewLine + @"  /R:RemoteSSHFolderPath   Full Path of Remote SSH directory (./ for home dir)";
            strUsage += Environment.NewLine + @"  /D:LocalDestinationPath  Full Path of destination folder on local machine";
            strUsage += Environment.NewLine + @"  /C:CatalogFile           Full Path of catalog file";
            strUsage += Environment.NewLine + @"  /CR:CatalogFileRows      The number of rows to keep in the catalog file";
            strUsage += Environment.NewLine + @"                                  Default : " + DefaultCatalogFileRows;
            strUsage += Environment.NewLine + @"  /W:WildCard              Wildcard used for retrieving files";
            strUsage += Environment.NewLine + @"                                  Default : " + DefaultWildCard;
            strUsage += Environment.NewLine + @"  /T:Timeout               Timeout in milliseconds for SSH connection";
            strUsage += Environment.NewLine + @"                                  Default : " + DefaultTimeout;
            strUsage += Environment.NewLine + @"  /test                    Test connection, authentication and paths";
            strUsage += Environment.NewLine + @"                                  No files will be transmitted";
            strUsage += Environment.NewLine + @"  /debug                   Display additional debug information.";
            strUsage += Environment.NewLine + @"  /V                       Displays version and copyright information.";
            strUsage += Environment.NewLine + @"  /?                       Displays this help page.";
            strUsage += Environment.NewLine;
            strUsage += Environment.NewLine + @"Example :";
            strUsage += Environment.NewLine + @"SshSync.exe /S:servername /PN:12 /C:myprivatekey.txt /U:sftpuser /R:/HOME/LOGS /W:*.log /T:30000 /D:C:\incoming";
            strUsage += Environment.NewLine + "Version: 1.0g" + " (Feb 2009)";
            return strUsage;
        }

        public static String SetCopyrightDetails()
        {
            StringBuilder CopyrightNotice = new StringBuilder();
            CopyrightNotice.AppendLine(" ====================================================================");
            CopyrightNotice.AppendLine(" ==         SshSync Copyright 2007 Colin Todd, ToddSoft            ==");
            CopyrightNotice.AppendLine(" ====================================================================");

            CopyrightNotice.AppendLine(@" Redistribution and use in source and binary forms, with or without");
            CopyrightNotice.AppendLine(@" modification, are permitted provided that the following conditions are met:");

            CopyrightNotice.AppendLine(@" 1. Redistributions of source code must retain the above copyright notice,");
            CopyrightNotice.AppendLine(@" this list of conditions and the following disclaimer.");
            CopyrightNotice.AppendLine(@"");
            CopyrightNotice.AppendLine(@" 2. Redistributions in binary form must reproduce the above copyright ");
            CopyrightNotice.AppendLine(@" notice, this list of conditions and the following disclaimer in ");
            CopyrightNotice.AppendLine(@" the documentation and/or other materials provided with the distribution.");
            CopyrightNotice.AppendLine(@"");
            CopyrightNotice.AppendLine(@" 3. The names of the authors may not be used to endorse or promote products");
            CopyrightNotice.AppendLine(@" derived from this software without specific prior written permission.");
            CopyrightNotice.AppendLine(@"");
            CopyrightNotice.AppendLine(@" THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,");
            CopyrightNotice.AppendLine(@" INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND");
            CopyrightNotice.AppendLine(@" FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR");
            CopyrightNotice.AppendLine(@" OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,");
            CopyrightNotice.AppendLine(@" INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT");
            CopyrightNotice.AppendLine(@" LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,");
            CopyrightNotice.AppendLine(@" OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF");
            CopyrightNotice.AppendLine(@" LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING");
            CopyrightNotice.AppendLine(@" NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,");
            CopyrightNotice.AppendLine(@" EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.");
            CopyrightNotice.AppendLine(@"");


            CopyrightNotice.AppendLine(" ====================================================================");
            CopyrightNotice.AppendLine(" ==       SharpSSH Copyright Tamir Gal (c) 2005 & jcraft.com       ==");
            CopyrightNotice.AppendLine(" ====================================================================");
            CopyrightNotice.AppendLine();
            
            
            CopyrightNotice.AppendLine(@" Copyright (c) 2006 Tamir Gal, http://www.tamirgal.com, All rights reserved.");
            CopyrightNotice.AppendLine(@" ");
            CopyrightNotice.AppendLine(@" Redistribution and use in source and binary forms, with or without");
            CopyrightNotice.AppendLine(@" modification, are permitted provided that the following conditions are met:");
            CopyrightNotice.AppendLine(@"");
            CopyrightNotice.AppendLine(@" 1. Redistributions of source code must retain the above copyright notice,");
            CopyrightNotice.AppendLine(@" this list of conditions and the following disclaimer.");
            CopyrightNotice.AppendLine(@"");
            CopyrightNotice.AppendLine(@" 2. Redistributions in binary form must reproduce the above copyright ");
            CopyrightNotice.AppendLine(@" notice, this list of conditions and the following disclaimer in ");
            CopyrightNotice.AppendLine(@" the documentation and/or other materials provided with the distribution.");
            CopyrightNotice.AppendLine(@"");
            CopyrightNotice.AppendLine(@" 3. The names of the authors may not be used to endorse or promote products");
            CopyrightNotice.AppendLine(@" derived from this software without specific prior written permission.");
            CopyrightNotice.AppendLine(@"");
            CopyrightNotice.AppendLine(@" THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,");
            CopyrightNotice.AppendLine(@" INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND");
            CopyrightNotice.AppendLine(@" FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR");
            CopyrightNotice.AppendLine(@" OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,");
            CopyrightNotice.AppendLine(@" INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT");
            CopyrightNotice.AppendLine(@" LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,");
            CopyrightNotice.AppendLine(@" OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF");
            CopyrightNotice.AppendLine(@" LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING");
            CopyrightNotice.AppendLine(@" NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,");
            CopyrightNotice.AppendLine(@" EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.");

            return CopyrightNotice.ToString();

        }
    }

}
