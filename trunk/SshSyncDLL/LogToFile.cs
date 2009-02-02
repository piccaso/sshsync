using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Configuration;

namespace Toddsoft.SSH
{
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



    /// <summary>
    /// Summary description for Util.
    /// </summary>
    public class Util
    {
        //        static String _DefaultLogToFile = @"C:\temp\ToddSoft_LogToFile.log";

        #region LOG TO FILE

        /// <summary>
        /// Write output message to a log file
        /// </summary>
        /// <param name="DebugOn">A boolean if set to TRUE, then the text will be written. If set to FALSE, no text will be written</param>
        /// <param name="Message">A string containing text to be written to the file</param>
        public static void LogToFile(Boolean bDebugOn, String Message)
        {
            LogToFile(false, "", Message, false, true);
        }




        /// <summary>
        /// Write output message to a log file
        /// </summary>
        /// <param name="DebugOn">A boolean if set to TRUE, then the text will be written. If set to FALSE, no text will be written</param>
        /// <param name="FileName">A string containing the path of the text file to write to</param>
        /// <param name="Message">A string containing text to be written to the file</param>
        /// <param name="bAddTimStamp">A boolean if set to TRUE, will mean the current Date timestamp will proceed the message text</param>
        public static void LogToFile(Boolean bDebugOn, String FileName, String Message, Boolean bAddTimStamp, Boolean bEchoToConsole)
        {
            String dtNow = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            String space = "     ";

            if (bDebugOn)
            {
                if (!bAddTimStamp)
                {
                    dtNow = "";
                    space = "";
                }

                // This text is added only once to the file.
                if (!File.Exists(FileName))
                {
                    //ensure directory exists
                    if (!Directory.Exists(new FileInfo(FileName).DirectoryName))
                    {
                        Directory.CreateDirectory(new FileInfo(FileName).DirectoryName);
                    }

                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(FileName))
                    {
                        sw.Write("");
                        //sw.WriteLine(dtNow + space + "Start Of Log");
                        //sw.WriteLine("-----------------------------------------------");
                    }
                }

                // This text is always added, making the file longer over time
                // if it is not deleted.
                try
                {
                    using (StreamWriter sw = File.AppendText(FileName))
                    {
                        sw.WriteLine(dtNow + space + Message.ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: Writing to log file." + e.Message);
                    Console.WriteLine("Original logging details: " + dtNow + space + Message.ToString());
                }
            }//if
            if (bEchoToConsole)
            {
                Console.WriteLine(dtNow + space + Message.ToString());
            }
        }

        /// <summary>
        /// Derive the filename of the log file to write to
        /// </summary>
        /// <returns>The file path of the output log file</returns>
        private static String GetLogFileName(String ComponentName)
        {
            String LogFilePath;
            String strDate;
            String LoggingDirectory;
            String ApplicationName;

            //read Output filename and path, service name from app.config settings
            AppSettingsReader reader = new AppSettingsReader();

            LoggingDirectory = (String)reader.GetValue("LoggingDirectory", String.Empty.GetType());
            ApplicationName = (String)reader.GetValue("ApplicationName", String.Empty.GetType());

            strDate = DateTime.Now.ToString("yyyyMMdd");

            LogFilePath = LoggingDirectory;
            if (!LoggingDirectory.EndsWith(@"\"))
            {
                LogFilePath = LogFilePath + @"\";
            }
            LogFilePath = LogFilePath + ApplicationName + "_" + ComponentName + "_" + strDate + ".log";

            return LogFilePath;
        }


        #endregion

    }




}
