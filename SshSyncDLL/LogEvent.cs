using System;
using System.Diagnostics;
using System.Collections;

namespace ToddSoft.Tools
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
    public partial class Util
    {

        #region LOG EVENT

        /// <summary>
        /// Logs a message to the Windows Event Log
        /// </summary>
        /// <param name="SourceName">A string containing the Source of the message</param>
        /// <param name="EventLogMessage">A string contain the text message</param>
        /// <param name="oEventLogEntryType">An EventLogEntryType object referring to the type of message (e.g. Information, SuccessAudit, Warning etc).</param>
        /// <param name="iEventID">An integer containing a number that may represent the error code for the message</param>
        public static void LogEvent(String SourceName, String EventLogMessage, EventLogEntryType oEventLogEntryType, int iEventID)
        {
            // Write to "Application" log by default
            LogEvent(SourceName, EventLogMessage, "Application", oEventLogEntryType, iEventID);
        }

        /// <summary>
        /// Logs a message to the Windows Event Log
        /// </summary>
        /// <param name="SourceName">A string containing the Source of the message</param>
        /// <param name="EventLogMessage">A string contain the text message</param>
        /// <param name="LogName">The name of the Windows Event Log to write to (can be Application, Security, System or a custom log name)</param>
        /// <param name="oEventLogEntryType">An EventLogEntryType object referring to the type of message (e.g. Information, SuccessAudit, Warning etc).</param>
        /// <param name="iEventID">An integer containing a number that may represent the error code for the message</param>
        public static void LogEvent(String SourceName, String EventLogMessage, String LogName, EventLogEntryType oEventLogEntryType, int iEventID)
        {
            // Create an EventLog instance and assign its source.
            EventLog myLog = new EventLog();
            myLog.Log = LogName;
            myLog.MachineName = Environment.MachineName;
            myLog.Source = SourceName;

            // Write an informational entry to the event log.    
            myLog.WriteEntry(EventLogMessage, oEventLogEntryType, iEventID);
        }
        #endregion

    }
}
