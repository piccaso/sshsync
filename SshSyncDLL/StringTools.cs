using System;
using System.Text;
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

        #region String Tools
        /// <summary>
        /// Appends a trailing slash to the input string if it doesn't already exist
        /// </summary>
        /// <param name="InputString">A string to which a trailing slash will be added, if it doesn't already exist</param>
        /// <param name="slash">A char containing the slash character to be added</param>
        /// <returns></returns>
        public static String AddTrailingSlash(String InputString, char slash)
        {
            if (!InputString.Substring(InputString.Length - 1, 1).Equals(slash.ToString()))
            {
                InputString = InputString + slash.ToString();
            }
            return InputString;
        }

        /// <summary>
        /// Removes a trailing slash ('/' or '\') from the input string if it exists
        /// </summary>
        /// <param name="InputString">A string to which the trailing slash will be removed, if it exists</param>
        /// <returns>A string with the trailing slash removed</returns>
        public static String RemoveSlash(String InputString)
        {
            String Slash1 = @"\";
            String Slash2 = @"/";
            if (InputString.Length > 0)
            {

                if ((InputString.Substring(InputString.Length - 1, 1).Equals(Slash1)) || (InputString.Substring(InputString.Length - 1, 1).Equals(Slash2)))
                {
                    InputString = InputString.Substring(0, InputString.Length - 1);
                }

                if ((InputString.Substring(0, 1).Equals(Slash1)) || (InputString.Substring(0, 1).Equals(Slash2)))
                {
                    InputString = InputString.Substring(1);
                }
            }
            return InputString;
        }


        public static string AddCommas(long Number)
        {

            String strNumber = Number.ToString();

            int len = strNumber.Length;
            int count = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = len - 1; i >= 0; i--)
            {
                if (count % 3 == 0 && count !=0) sb.Insert(0,",");
                sb.Insert(0, strNumber.Substring(i, 1));
                //Console.Write(strNumber.Substring(i, 1));
                count++;
            }
            //Console.WriteLine(sb.ToString());
            return sb.ToString();

        }

        /// <summary>
        /// This helper method is used to split a user-defined delimted string into parts
        /// Some jiggery is require when there are quotes (") in the field
        /// 
        /// </summary>
        /// <param name="myStringToSplit">The string to split</param>
        /// <param name="SplitChar">The char used to split the string</param>
        /// <returns>An array containing the split strings</returns>
        public static String[] SplitString(String myStringToSplit, char SplitChar)
        {
            String text1 = myStringToSplit;
            String[] text2 = text1.Split(SplitChar);
            String[] text3 = new String[text2.Length];
            String[] text4 = null;
            int i = 0;
            bool inOpenQuote = false;


            foreach (String text in text2)
            {
                if (inOpenQuote)
                {
                    i--;
                    text3[i] = String.Concat(text3[i], SplitChar.ToString() + " ", text);
                }
                else
                    text3[i] = text;
                i++;
                if (text.Trim().StartsWith("\""))
                {
                    inOpenQuote = true;
                }
                if (text.Trim().EndsWith("\""))
                {
                    inOpenQuote = false;
                }
            }// foreach

            text4 = new String[i];
            for (int index = 0; index < i; index++) text4[index] = text3[index];
            //            foreach (string text in text4) Console.WriteLine(text);
            return text4;
        }//SplitString()


        /// <summary>
        /// Returns a string of a fixed width either concatenated with spaces for truncated
        /// </summary>
        /// <param name="TheString">The string to print at a fixed width</param>
        /// <param name="maxLength">The length of the new string</param>
        /// <returns>A fixed width string</returns>
        public static String PrintFixedWidthString(String TheString, int maxLength)
        {
            return PrintFixedWidthString(TheString, maxLength, ' ');
        }



        /// <summary>
        /// Returns a string of a fixed width either concatenated with blanks for truncated
        /// </summary>
        /// <param name="TheString">The string to print at a fixed width</param>
        /// <param name="maxLength">The length of the new string</param>
        /// <param name="blanks">The character to pad the end of a string with</param>
        /// <returns>A fixed width string</returns>
        public static String PrintFixedWidthString(String TheString, int maxLength, char blanks)
        {
            StringBuilder output;
            if (TheString == null)
            {
                TheString = "";
            }
            if (TheString.Length >= maxLength)
            {
                return TheString.Substring(0, maxLength);
            }
            else
            {
                output = new StringBuilder(TheString);
                while (output.Length < maxLength)
                {
                    output.Append(blanks.ToString());
                }
                return output.ToString();
            }
        }



        #endregion
    }
}
