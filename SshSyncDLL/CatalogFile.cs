using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.VisualBasic;
using ToddSoft.Tools;
/*
 * 
 *   ====================================================================
 *   ==     ToddSoft library Copyright 2007 Colin Todd, ToddSoft       ==
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
*/


namespace ToddSoft.Tools
{
    public class CatalogFile
    {

        public const int SYNC_ACCESSCATFILEERR = 3999;

        private FileInfo _fiCatalogFile;
        private int _MaxCatalogFileRows;
        private Boolean _bDebugOn = false;
        private Boolean _bUseCatalogFile = true;
        private Dictionary<string, double> FileEntries =  new Dictionary<string, double>();

        public int count;

        public CatalogFile(Boolean bUseCatalogFile)
        {
            _bUseCatalogFile = bUseCatalogFile;

            //turn off debug by default
            _bDebugOn = false;
            _MaxCatalogFileRows = 0;
        }

        public CatalogFile(Boolean bUseCatalogFile, Boolean bDebugMode)
        {
            _bUseCatalogFile = bUseCatalogFile;
            _bDebugOn = bDebugMode;
            _MaxCatalogFileRows = 0;
        }


        public int MaxCatalogFileRows {
            get { return _MaxCatalogFileRows; }
            set { _MaxCatalogFileRows = value; }
        }


        public void SetCatalogFileName(String FileName)
        {
            if (_bUseCatalogFile)
            {
                try
                {
                    //Check that CatalogFile exists
                    _fiCatalogFile = new FileInfo(FileName);
                    ToddSoft.Tools.Util.Debug("Setting up new CatalogFile object for  : " + FileName,_bDebugOn);
                    if (File.Exists(FileName))
                    {
                        // already exists
                        ToddSoft.Tools.Util.Debug("CatalogFile already exists", _bDebugOn);

                    }
                    else
                    {
                        // If it doesn't exist, then create a blank file
                        ToddSoft.Tools.Util.Debug("CatalogFile does not exist. Creating new file.", _bDebugOn);
                        _fiCatalogFile.Create();
                    }
                }
                catch (Exception e)
                {
                    ToddSoft.Tools.Util.Debug("Error accessing catalog file :" + FileName + ". Quitting", CatalogFile.SYNC_ACCESSCATFILEERR, true);
                    ToddSoft.Tools.Util.LogEvent("SshSync", "Error accessing catalog file :" + FileName + "." + e.Message, EventLogEntryType.Error, CatalogFile.SYNC_ACCESSCATFILEERR);
                    Environment.Exit(CatalogFile.SYNC_ACCESSCATFILEERR);
                }
            }
        }

        public void Load()
        {

            /*
             * The catalog file needs to be in the below format
             * filename.ext,filesizebytes
             * 
             * The file needs to be comma delimitered.
             * Blank lines are allowed
             * The FileName should not include path
             * The FileSize should be a number representing the byte count. It will need to be converted to a double
             * Duplicate lines are allowed. Only the latest entry will be loaded to the cache
             * 
             */ 
            
            
            if (_bUseCatalogFile){

                try
                {
                    //only proceed if the file contains entries
                    if (this._fiCatalogFile.Length > 1)
                    {
                        // Create an instance of StreamReader to read from a file.
                        // The using statement also closes the StreamReader.
                        using (StreamReader sr = new StreamReader(this._fiCatalogFile.FullName))
                        {
                            String line;
                            // Read and display lines from the file until the end of 
                            // the file is reached.
                            ToddSoft.Tools.Util.Debug("Loading Catalog file to internal cache...", _bDebugOn);

                            while ((line = sr.ReadLine()) != null)
                            {
                                String[] lineSplit = line.Split(',');
                                String FileName = lineSplit[0];
                                //Console.WriteLine("[0] " + lineSplit[0]);
                                //Console.WriteLine("[1] " + lineSplit[1]);

                                try
                                {

                                    double dFileSize = Convert.ToDouble(lineSplit[1]);

                                    ToddSoft.Tools.Util.Debug("Adding : '" + FileName + "' : " + dFileSize.ToString() + " bytes", _bDebugOn);
                                    //check if entry already exists
                                    if (FileEntries.ContainsKey(FileName))
                                    {
                                        // Remove duplicate before adding 'latest' details
                                        ToddSoft.Tools.Util.Debug("Removing duplicate entry (" + FileName + ") in file catalog cache", _bDebugOn);
                                        FileEntries.Remove(FileName);
                                    }
                                    // Add the cache
                                    FileEntries.Add(FileName, dFileSize);
                                }
                                catch
                                {
                                    ToddSoft.Tools.Util.Debug("Error while adding entry to local file (" + FileName + ") to catalog cache", _bDebugOn);
                                }
                            }
                            //Close off the reader objects
                            sr.Close();
                            sr.Dispose();
                        }//using
                    }
                    else
                    {
                        ToddSoft.Tools.Util.Debug("Catalog file is empty...", _bDebugOn);
                    }
                    this.count = FileEntries.Count;
                    ToddSoft.Tools.Util.Debug("Total entries loaded : " + this.count, _bDebugOn);
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    ToddSoft.Tools.Util.LogEvent("SshSync", "Error opening catalog file :" + _fiCatalogFile.FullName + ". " + e.Message, EventLogEntryType.Error, CatalogFile.SYNC_ACCESSCATFILEERR);
                    ToddSoft.Tools.Util.Debug("The catalog file (" + _fiCatalogFile.FullName + ") could not be read. Quitting.", _bDebugOn);
                    Environment.Exit(CatalogFile.SYNC_ACCESSCATFILEERR);
                }
            }
        }

        public Boolean CheckCatalogEntryExists(String FileName, double FileSize)
        {
            Boolean bExists = false;
            Double FileSizeOut;

            if (_bUseCatalogFile)
            {
                ToddSoft.Tools.Util.Debug("Checking Catalog File for entry", _bDebugOn);
                if (FileEntries.TryGetValue(FileName, out FileSizeOut))
                {
                    if (FileSize.Equals(FileSizeOut))
                    {
                        ToddSoft.Tools.Util.Debug("FileName / FileSize already exists in catalog", _bDebugOn);
                        // exists and file size is the same
                        bExists = true;
                    }
                    else
                    {
                        // exists but file size is different
                        ToddSoft.Tools.Util.Debug("FileName exists but file size is different in catalog", _bDebugOn);
                        bExists = false;
                    }
                }
                else
                {
                    // File Not in catalog file
                    ToddSoft.Tools.Util.Debug("File does not exist in catalog", _bDebugOn);
                    bExists = false;
                }
                return bExists;
            }
            else
            {
                return false;
            }
        }

        public void AddToCatalog(String FileName,double FileSize)
        {
            if (_bUseCatalogFile)
            {
                //add to file catalog
                try
                {
                    //Write file details to catalog file 
                    using (StreamWriter w = File.AppendText(this._fiCatalogFile.FullName))
                    {
                        w.Write(Environment.NewLine + FileName + "," + FileSize.ToString());
                        //w.WriteLine(FileName + "," + FileSize.ToString());
                        // Close the writer and underlying file.
                        w.Flush();
                        w.Close();
                    }
                }
                catch (Exception e)
                {
                    ToddSoft.Tools.Util.Debug("Error writing to the catalog file :" + _fiCatalogFile.FullName + ". Quiting", true);
                    Util.LogEvent("SshSync", "Error writing to the catalog file :" + _fiCatalogFile.FullName + "." + e.Message, EventLogEntryType.Error, CatalogFile.SYNC_ACCESSCATFILEERR);
                    Environment.Exit(CatalogFile.SYNC_ACCESSCATFILEERR);
                }

                //add to internal cache
                //Check for duplicate FileName
                if (FileEntries.ContainsKey(FileName))
                {
                    //Remove Duplicate before adding new entry
                    FileEntries.Remove(FileName);
                }

                FileEntries.Add(FileName, FileSize);
                // update public counter
                this.count = FileEntries.Count;

                ToddSoft.Tools.Util.Debug("File has been added to the catalog and internal cache", _bDebugOn);
            }
        }

        public int CleanUpCatalogFile()
        {
            int iRowsRemovedCatalogFile = 0;
            //write internal cache to disk

           
            Queue<String> sb = new Queue<String>();

            if (_MaxCatalogFileRows > 0)
            {
                using (StreamReader sr = new StreamReader(this._fiCatalogFile.FullName))
                {
                    String line;
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    ToddSoft.Tools.Util.Debug("Loading Catalog file to internal cache...", _bDebugOn);

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Trim().Length > 0){
                            sb.Enqueue(line.Trim());
                        }
                    }
                    //Close off the reader objects
                    sr.Close();
                    sr.Dispose();
                }//using

                //dequeue what we need to remove
                if (sb.Count <= _MaxCatalogFileRows) return 0;
                
                while (sb.Count > _MaxCatalogFileRows)
                {
                    sb.Dequeue();
                    iRowsRemovedCatalogFile++;
                }

                //now write to disk

                using (StreamWriter w = File.CreateText(this._fiCatalogFile.FullName))
                {
                    while (sb.Count > 0)
                    {
                        w.WriteLine(sb.Dequeue());
                    }
                    w.Flush();
                    w.Close();
                }
            }
            return iRowsRemovedCatalogFile;
        }
    }
}
