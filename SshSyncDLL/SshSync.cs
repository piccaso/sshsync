using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using Tamir.SharpSsh;
using Tamir.SharpSsh.jsch;
using Tamir.SharpSsh.java.util;
using ToddSoft.Tools;
using System.Threading;

/*
 * SshSync
 * 
 * Description:-
 *   A tool to allow intelligent Secure FTP transmissions. SshSync only support pull type transfers, but it allows 
 *   use of a Private Key to ensure that authentication  is secure. A text file that contains a list of files always 
 *   processed is used to check that only 'new' files are retrieved.
 * 
 * Features:-
 *   - SFTP Pull over SSH
 *   - Fully configurable input parameter, including port number
 *   - Password or public key authentication are the only authentication type allowed
 *   - Test mode enables diagnostics without actually pulling over any files
 *   - .Net 2.0 managed code allows easy integration into other projects
 * 
 * 
 * 
 * 
 * ERROR CODES 
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
 * All events are logged to Windows Application logs under the application name SshSync
 * 
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
 **/

namespace Toddsoft.SSH
{   
    public class SshSync
    {
        #region Error Codes
        public const int SSHSYNC_OK = 0;
        public const int SSHSYNC_SSHCONNERR = 3001;
        public const int SSHSYNC_RETRFILEERR = 3002;
        public const int SSHSYNC_SSHUNKERR = 3003;
        public const int SSHSYNC_WRITEDESTERR = 3004;
        public const int SSHSYNC_ACCESSCATFILEERR = 3005;
        public const int SSHSYNC_SSHCONNTIMEOUT = 3006;
        public const int SSHSYNC_SSHDISCNTERR = 3008;
        public const int SSHSYNC_INVLDPARAMTRS = 3009;
        public const int SSHSYNC_INVLDCATFILE = 3010;
        public const int SSHSYNC_INVDAUTHDTLS = 3011;
        public const int SSHSYNC_UNKNOWNERROR = 3099;
        #endregion

        public static int ConnectionStatus;  // required for TimeOut Check
        internal const int SSHNOTCONNECTED = 0;
        internal const int SSHCONNECTED = 1;

        private String _HostName;
        private int _PortNumber;
        private String _UserName;
        private String _Password;
        private String _PrivateKey;
        private String _FileCatalog;
        private int _MaxFileCatalogRows;
        private String _SFTPFilePath;
        private String _FileWildCard;
        private String _DestFolder;
        private int _iConnectionTimeout;
        private Boolean _bDebugOn;
        private Boolean _bTestConnection;
        private Boolean _bUseCatalogFile;

        private Sftp _SFTPConnection;
        private CatalogFile _oCatalogFile;

        public SshSync(String HostName, int PortNumber, String UserName, String Password, String PrivateKey,
            String FileCatalog, int MaxFileCatalogRows, String SFTPFilePath, String FileWildCard, String DestFolder, int iConnectionTimeout, 
            Boolean bDebugOn, Boolean bTestConnection)
        {
            _HostName = HostName;
            _PortNumber = PortNumber;
            _UserName = UserName;
            _Password = Password;
            _PrivateKey = PrivateKey;
            _FileCatalog = FileCatalog;
            _MaxFileCatalogRows = MaxFileCatalogRows;

            if (_FileCatalog.Length > 0)
            {
                _bUseCatalogFile = true;
            }
            else
            {
                _bUseCatalogFile = false;
            }
            
            _SFTPFilePath = SFTPFilePath;
            _FileWildCard = FileWildCard;
            _DestFolder = DestFolder;
            _iConnectionTimeout = iConnectionTimeout;
            _bDebugOn = bDebugOn;
            _bTestConnection = bTestConnection;

            //Create Catalog Object and Initialize
            _oCatalogFile = new CatalogFile(_bUseCatalogFile, _bDebugOn);
            _oCatalogFile.SetCatalogFileName(_FileCatalog);
            _oCatalogFile.MaxCatalogFileRows = _MaxFileCatalogRows;

            // load catalog file to internal cache
            _oCatalogFile.Load();

            if ((_bUseCatalogFile) && _bTestConnection) ToddSoft.Tools.Util.Debug(_bDebugOn,"Catalog File appears intact. " + _oCatalogFile.count + " entries loaded");
            if ((!_bUseCatalogFile) && _bTestConnection) ToddSoft.Tools.Util.Debug(_bDebugOn,"Using non-Catalog File retrieval. ");

        }

        public int PerformSSHSync()
        {
            Boolean FileExistsInCatalog = false;
            String SSHFileName;
            String DestinationFolder = _DestFolder;
            double SSHFileSize;
            SftpATTRS oSftpATTRS;
            Vector oFileListing;

            try
            {

                #region FixFilePath
                // append slash if missing
                //Debug("append slash if missing", _bDebugOn);
                _SFTPFilePath = ToddSoft.Tools.Util.AddTrailingSlash(_SFTPFilePath, '/');
                DestinationFolder = ToddSoft.Tools.Util.AddTrailingSlash(DestinationFolder, '\\');
                #endregion

                #region Test Destination Folder
                if (_bTestConnection)
                {
                    System.Random r = new System.Random((DateTime.Now.Millisecond));
                    String TempFileName = DestinationFolder + r.Next();
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"Testing writing to destination folder : " + DestinationFolder);
                    // Create the file.
                    using (FileStream TestDestinationfs = File.Create(TempFileName))
                    {
                        Byte[] info = new System.Text.UTF8Encoding(true).GetBytes("This is some text in the file.");
                        // Add some information to the file.
                        TestDestinationfs.Write(info, 0, info.Length);
                        TestDestinationfs.Flush();
                        TestDestinationfs.Close();
                        if (!File.Exists(TempFileName))
                        {
                            ToddSoft.Tools.Util.Debug(_bDebugOn,"Error writing to destination folder : " + DestinationFolder);
                            ToddSoft.Tools.Util.LogEvent("SshSync", "Error writing to destination folder", EventLogEntryType.Error, SSHSYNC_WRITEDESTERR);
                            return SSHSYNC_WRITEDESTERR;
                        }
                        else
                        {
                            ToddSoft.Tools.Util.Debug(_bDebugOn,"Write file to Destination folder successful");
                        }
                    }

                    // Open the stream and read it back.
                    using (StreamReader TestDestinationSr = File.OpenText(TempFileName))
                    {
                        int counter = 0;
                        string s = "";
                        while ((s = TestDestinationSr.ReadLine()) != null)
                        {
                            counter++;
                        }
                        TestDestinationSr.Close();
                        ToddSoft.Tools.Util.Debug(_bDebugOn,counter + " lines read from catalog file.");
                        
                    }
                    File.Delete(TempFileName);
                    if (File.Exists(TempFileName))
                    {
                        ToddSoft.Tools.Util.Debug(_bDebugOn,"Error deleting file from destination folder : " + DestinationFolder);
                        // this is not a fatal error. Continue with execution.
                    }
                    else
                    {
                        ToddSoft.Tools.Util.Debug(_bDebugOn,"Delete file from destination folder was successful");
                    }
                }   
                #endregion

                #region SSH Connection
                ToddSoft.Tools.Util.Debug(_bDebugOn,"---------------------");
                
                // Set up SFTP Connection
                _SFTPConnection = new Sftp(_HostName, _UserName);

                //Determine what authentication will be used
                if (_Password.Length > 0 ) 
                {
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"Using Password Authentication");
                    _SFTPConnection.Password = _Password; 
                }
                if (_PrivateKey.Length >  0) 
                {
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"Using Public Key Authentication");
                    #region Check PrivateKey

                    FileInfo fi = new FileInfo(_PrivateKey);
                    
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"Checking Private Key file : " + _PrivateKey);
                    if (fi.Exists)
                    {
                        ToddSoft.Tools.Util.Debug(_bDebugOn,"Private Key file exists");
                        _SFTPConnection.AddIdentityFile(_PrivateKey);
                    }
                    else
                    {
                        ToddSoft.Tools.Util.Debug(_bDebugOn,"Private Key file does not exist");
                        if (_Password.Length == 0)
                        {
                            ToddSoft.Tools.Util.LogEvent("SshSync", "Private (" + fi.FullName + ") Key does not exist and password is empty.", EventLogEntryType.Error, SSHSYNC_SSHCONNERR);
                            ToddSoft.Tools.Util.Debug(_bDebugOn,"Public Key and password details are invalid. Quitting...");
                            return (SSHSYNC_INVDAUTHDTLS);
                        }
                    
                    }
                        
                    #endregion
                }

                // Connect to SSH server
                ToddSoft.Tools.Util.Debug(_bDebugOn,"Connecting to " + _HostName + ":" + _PortNumber.ToString() + "");

                // not connected
                ConnectionStatus = SSHNOTCONNECTED; 
                // Launch new thread to check for Timeouts
                Thread TimeOutThread = new Thread(new ParameterizedThreadStart( StartTimeOut));
                // Start Timeout thread
                TimeOutThread.Name = "SSHSync_TimeOutThread";
                TimeOutThread.Start((object)Thread.CurrentThread);

                _SFTPConnection.Connect(_PortNumber);
                #endregion

                #region SSH Retrieval
                if (!_SFTPConnection.Connected)
                {
                    //The connection failed (This was NOT caused by a time out)
                    ConnectionStatus = SSHNOTCONNECTED;
                    // Kill the TimeOut Thread
                    TimeOutThread.Abort();
                    //raise error
//                    ToddSoft.Tools.Util.LogEvent("SshSync", "Connection to " + _HostName + ":" + _PortNumber + " failed", EventLogEntryType.Error, SSHSYNC_SSHCONNERR);
                    if (_bTestConnection) ToddSoft.Tools.Util.Debug(_bDebugOn,"Connection fail.");
                    //Return
                    return SSHSYNC_SSHCONNERR;
                }
                else
                {
                    // If execution gets to this point, the connection was successful and didn't exceed the timeout value
                    ConnectionStatus = SSHCONNECTED;
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"Connection to " + _HostName + ":" + _PortNumber.ToString() + " was Successful!");

                    //Retrieve file listing of SSH directory
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"Attempting to retrieve file listing...");
                    oFileListing = _SFTPConnection.ls(_SFTPFilePath + _FileWildCard);

                    ToddSoft.Tools.Util.Debug(_bDebugOn,"Retrieved Directory Listing for " + _SFTPFilePath + _FileWildCard + " . " + oFileListing.Count + " file/s found");
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"---------------------");
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"*******************************");
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"*** RUNNING UNDER TEST MODE ***");
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"*******************************");
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"*** NO FILES WILL BE COPIED ***");
                    ToddSoft.Tools.Util.Debug(_bDebugOn,"*******************************");


                    #region Traverse File Listing
                    // Iterate through each entry in the directory listing
                    // for possible files to retrieve
                    foreach (ChannelSftp.LsEntry oFileListingEntry in oFileListing)
                    {
                        ToddSoft.Tools.Util.Debug(_bDebugOn,"Found : " + oFileListingEntry.getFilename() + " " + oFileListingEntry.getAttrs());

                        // retrieve FileName
                        SSHFileName = oFileListingEntry.getFilename();

                        // retrieve FileSize
                        oSftpATTRS = oFileListingEntry.getAttrs();
                        oSftpATTRS.getSize();
                        SSHFileSize = oSftpATTRS.getSize();

                        if (SSHFileName.Equals(".") || SSHFileName.Equals("..") || oSftpATTRS.isDir())
                        {
                            // ignore these directories
                        }
                        else
                        {
                            if (_bUseCatalogFile)
                            {
                                //Check each file against the catalog file.
                                FileExistsInCatalog = _oCatalogFile.CheckCatalogEntryExists(SSHFileName, SSHFileSize);
                            }
                            else
                            {
                                //force retrieval of file if not using catalog file
                                FileExistsInCatalog = false;
                            }

                            if (FileExistsInCatalog)
                            {
                                // File Exists in Catalog
                                ToddSoft.Tools.Util.Debug(_bDebugOn,"Skipping '" + SSHFileName + "'. Already exists in catalog");
                                // Skip to next file
                            }
                            else
                            {
                                // File does NOT exist in Catalog
                                // Retrieve file via SFTP
                                ToddSoft.Tools.Util.Debug(_bDebugOn,"Retrieving '" + SSHFileName + "'...");

                                // Do not retrieve if we are in test mode
                                if (!_bTestConnection)
                                {
                                    ToddSoft.Tools.Util.Debug(_bDebugOn,"Retrieving file...");

                                    try
                                    {

                                        //Retrieve file by SFTP
                                        _SFTPConnection.Get(_SFTPFilePath + SSHFileName, DestinationFolder);
                                    }
                                    catch (Exception ee)
                                    {
                                        ToddSoft.Tools.Util.Debug(_bDebugOn, "Error retrieving file : " + _SFTPFilePath + SSHFileName);
                                    }

                                        //Check that file was received correctly
                                        FileInfo fiDestFile = new FileInfo(DestinationFolder + SSHFileName);

                                    // Check file exists and the size matches the original on the SSH server
                                    if (fiDestFile.Exists && fiDestFile.Length == Convert.ToInt64(SSHFileSize))
                                    {
                                        // retrieve was successful
                                        ToddSoft.Tools.Util.Debug(_bDebugOn,"Retrieved file successfully!");
                                        ToddSoft.Tools.Util.Debug(_bDebugOn,"File exists and filesize is the same as on the remote server");
                                        // add file to catalog 
                                        if (_bUseCatalogFile) _oCatalogFile.AddToCatalog(SSHFileName, SSHFileSize);
                                    }
                                    else
                                    {
                                        // retrieve was unsuccessful
                                        ToddSoft.Tools.Util.Debug(_bDebugOn,"Retrieve was NOT sucessful." + ". The file size was different to the size shown in the directory listing (file=" + fiDestFile.Length + ",dirlisting=" + Convert.ToInt64(SSHFileSize) + ")");
                                        //ToddSoft.Tools.Util.Debug(_bDebugOn,"Removing file : " + SSHFileName);
                                        //File.Delete(DestinationFolder + "\\" + SSHFileName);
                                        ToddSoft.Tools.Util.LogEvent("SshSync", "Error retrieving file '" + _SFTPFilePath + SSHFileName + "' from " + _HostName + ":" + _PortNumber + ". The file size was different to the size shown in the directory listing (file=" + fiDestFile.Length + ",dirlisting=" + Convert.ToInt64(SSHFileSize) + ")", EventLogEntryType.Information, SSHSYNC_RETRFILEERR);
                                    }
                                }//if
                            }//else
                            // go to next
                            ToddSoft.Tools.Util.Debug(_bDebugOn,"---------------------");
                        }//else
                    }//foreach
                    #endregion

                }//
                #endregion

            }
            catch (Exception e)
            {
                ToddSoft.Tools.Util.Debug(_bDebugOn,"Error (during SSH connection): " + e.Message);
                ToddSoft.Tools.Util.Debug(_bDebugOn,e.StackTrace);
//                ToddSoft.Tools.Util.LogEvent("SshSync", "Error occurred during SSH connection. " + e.Message, EventLogEntryType.Error, SSHSYNC_SSHUNKERR);
                return SSHSYNC_SSHUNKERR;
            }

            //clean up catalog file, ie remove old entries
            if (_bUseCatalogFile)
            {
                int iEntriesRemoved = 0;
                iEntriesRemoved = _oCatalogFile.CleanUpCatalogFile();
                ToddSoft.Tools.Util.Debug(_bDebugOn,"Removed " + iEntriesRemoved + " entries from the catalog file");
            }

            // No errors were encountered
            return SSHSYNC_OK;
        }

        public int Disconnect()
        {
            // Disconnect from SSH Server
            try
            {
                ToddSoft.Tools.Util.Debug(_bDebugOn,"Disconnecting...");
                _SFTPConnection.Close();
                ToddSoft.Tools.Util.Debug(_bDebugOn,"OK");
                return SSHSYNC_OK;
            }
            catch 
            {
                ToddSoft.Tools.Util.Debug(_bDebugOn,"Disconnect Failed.");
                return SSHSYNC_SSHDISCNTERR;            
            }
        }


        private void StartTimeOut(object oTimeout)
        {
            // This thread will wait oTimeOut before checking if the SSH connection was successful.
            Thread t = (Thread)oTimeout;
            int iTimeout = _iConnectionTimeout;
            Thread.Sleep(iTimeout);

            if (ConnectionStatus == 0)
            {
                // Connection has timed out. Quit.
                ToddSoft.Tools.Util.Debug(_bDebugOn,"Error: SSH Connection has timed out after " + iTimeout + " milliseconds.");
 //               ToddSoft.Tools.Util.LogEvent("SshSync", "SSH Connection has timed out after " + iTimeout + " milliseconds. Check SSH Server is functioning and is on correct port.", EventLogEntryType.Error, SSHSYNC_SSHCONNTIMEOUT);
                t.Abort();
                Thread.CurrentThread.Abort();
            }
        }
    }
}
