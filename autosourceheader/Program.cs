using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using neolibs.General;
using neolibs.FileUtils;
using System.IO;
using neolibs;
using neolibs.Extensions;
using Newtonsoft.Json;

namespace autosourceheader
{
    /// <summary>
    /// Open source notice files
    /// </summary>
    public class OpenSourceNoticeFile
    {
        /// <summary>
        /// File pattern to add open source notice to
        /// </summary>
        public string filepattern;

        /// <summary>
        /// Open source notice to use
        /// </summary>
        public string opensourcenotice;
    }

    /// <summary>
    /// Settings object
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Header filename of file contents to be put at the top of each file
        /// </summary>
        public string HeaderFilename { get; set;  }

        /// <summary>
        /// Header delimiter string, ie the string that tells us where the header ends
        /// </summary>
        public string HeaderDelimiter { get; set; }

        /// <summary>
        /// Absolute last line of source code
        /// </summary>
        public string LastLine { get; set; }

        /// <summary>
        /// List of files to include in operation
        /// </summary>
        public List<string> FilesList { get; set; }

        /// <summary>
        /// List of files to exclude
        /// </summary>
        public List<string> ExcludeList { get; set; }

        /// <summary>
        /// List of opensource notices
        /// </summary>
        public List<OpenSourceNoticeFile> OpenSourceNotices { get; set; }

        /// <summary>
        /// List of files to exclude
        /// </summary>
        public string SourceDirectory { get; set; }

        /// <summary>
        /// List of forbidden words in the code
        /// </summary>
        public List<string> ForbiddenWords { get; set; }

        /// <summary>
        /// Project name
        /// </summary>
        public string ProjectName { get; set;  }

        /// <summary>
        /// Compiler description
        /// </summary>
        public string Compiler { get; set; }

        /// <summary>
        /// Author description
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Project detailed description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Copyright
        /// </summary>
        public string Copyright { get; set; }

        /// <summary>
        /// Option to check for uncompleted TODOs
        /// </summary>
        public bool OptCheckTODOs { get; set; }  = true;

        /// <summary>
        /// Option to be verbose
        /// </summary>
        public bool OptVerbose { get; set; } = false;

        /// <summary>
        /// Option to Check for ASCII text only
        /// </summary>
        public bool OptCheckASCII { get; set; } = true;

        /// <summary>
        /// Option to report errors with full exception stack trace
        /// </summary>
        public bool OptLongErrors { get; set; } = false;

        /// <summary>
        /// Option to automatically add the last line comment
        /// </summary>
        public bool OptAutoAddLastLineComment { get; set; } = false;

        /// <summary>
        /// Option to autoamtically add the end of header comment
        /// </summary>
        public bool OptAutoAddEndOfHeaderComment { get; set; } = false;

        /// <summary>
        /// Clear the settings
        /// </summary>
        public void Clear()
        {
            ProjectName = "";
            Compiler = "";
            Author = "";
            Description = "";
            Copyright = "";

            HeaderFilename = "";
            HeaderDelimiter = "";
            LastLine = "";
            SourceDirectory = "";

            FilesList.Clear();
            ExcludeList.Clear();
            OpenSourceNotices.Clear();
            ForbiddenWords.Clear();
        }

        /// <summary>
        /// set defaults
        /// </summary>
        public void Defaults()
        {
            ProjectName = "Name of the project";
            Compiler = "Compiler and version";
            Author = "Author(s)";
            Description = "Descrioption of the project";
            Copyright = "(c) YEAR Copyright notice";

            HeaderFilename = "autosourceheader.txt";
            HeaderDelimiter = "/// End of Header";
            LastLine = "/// End of file";
            SourceDirectory = @"Directory\where\source\can\be\found";

            FilesList = new List<string>();
            FilesList.Add("*.hpp");
            FilesList.Add("*.cpp");
            FilesList.Add("*.c");
            FilesList.Add("*.h");

            ExcludeList = new List<string>();
            ExcludeList.Add("excluded" + Path.DirectorySeparatorChar);
            ExcludeList.Add("path" + Path.DirectorySeparatorChar);
            ExcludeList.Add("names" + Path.DirectorySeparatorChar);

            OpenSourceNotices = new List<OpenSourceNoticeFile>();

            ForbiddenWords = new List<string>();
            ForbiddenWords.Add("words");
            ForbiddenWords.Add("that");
            ForbiddenWords.Add("should");
            ForbiddenWords.Add("not");
            ForbiddenWords.Add("appear");
            ForbiddenWords.Add("in");
            ForbiddenWords.Add("the");
            ForbiddenWords.Add("code");
        }

        /// <summary>
        /// constructor
        /// </summary>
        public AppSettings()
        {
            FilesList = new List<string>();
            ExcludeList = new List<string>();
            OpenSourceNotices = new List<OpenSourceNoticeFile>();
            ForbiddenWords = new List<string>();
        }

        /// <summary>
        /// ToString method for AppSettings class
        /// </summary>
        /// <returns>string with description of the parameters</returns>
        public override string ToString()
        {
            string s = ReflectionUtils.GetClassPropertiesString(this);

            return s;
        }
    }

    class Program
    {
        private static AppSettings Settings = new AppSettings();

        /// <summary>
        /// Prepare the header
        /// </summary>
        /// <param name="headerlines">string array for header lines</param>
        /// <param name="filename">filename of the file being processed</param>
        /// <param name="dt">Current date and time</param>
        static void PrepareHeader(ref string[] headerlines, string filename, DateTime dt)
        {
            try
            {
                for (int i=0; i<headerlines.Count(); i++)
                {
                    headerlines[i] = headerlines[i].Replace("$PROJECT",  Settings.ProjectName);
                    headerlines[i] = headerlines[i].Replace("$COMPILER", Settings.Compiler);
                    headerlines[i] = headerlines[i].Replace("$FILE", Path.GetFileName(filename));
                    headerlines[i] = headerlines[i].Replace("$AUTHOR", Settings.Author);
                    headerlines[i] = headerlines[i].Replace("$DATE", dt.ToShortDateString() + ", " + dt.ToShortTimeString());
                    headerlines[i] = headerlines[i].Replace("$DESCRIPTION", Settings.Description);
                    headerlines[i] = headerlines[i].Replace("$COPYRIGHT", Settings.Copyright);
                    headerlines[i] = headerlines[i].Replace("$YEAR", dt.Year.ToString());
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Error in preparing Header file", exc);
            }
        }

        private enum EndOfFileStates
        {
            LookForCRLF,
            LookForEndofLineComment,
            Done
        }

        static void ProcessFile(string s)
        {
            try
            {
                List<string> Errors = new List<string>();
                int i;
                int linecount = 0;
                int hdrdelimidx = -1;
                DateTime timestamp = File.GetLastWriteTime(s);

                Console.WriteLine("Processing file " + s);

                string[] lines = File.ReadAllLines(s);
                string[] headerlines = File.ReadAllLines(Settings.HeaderFilename);

                if (Settings.OptVerbose)
                {
                    Console.WriteLine("Total Lines loaded: {0}",lines.Count());
                }
                // scan through all files and find the end of header text
                // while doing that also check for illegal words in the comments
                for (i = 0; i < lines.Count(); i++)
                {
                    linecount++;
                    if (lines[i] == Settings.HeaderDelimiter)
                    {
                        hdrdelimidx = i;
                    }

                    foreach(string forbiddenstr in Settings.ForbiddenWords)
                    {
                        if (lines[i].Contains(forbiddenstr,StringComparison.OrdinalIgnoreCase))
                        {
                            Errors.Add("Forbidden word " + forbiddenstr + " found at line " + linecount.ToString() + " in file " + s);
                        }
                    }
                    if (Settings.OptCheckTODOs)
                    {
                        if (lines[i].Contains("TODO",StringComparison.OrdinalIgnoreCase))
                        {
                            Errors.Add("TODO found at line " + linecount.ToString() + " in file " + s);
                        }
                    }
                    if (Settings.OptCheckASCII)
                    {
                        // ensure that the line contains ASCII characters only
                        if (!lines[i].IsASCIIOnly())
                        {
                            Errors.Add("Non ASCII characters found at line " + linecount.ToString() + " in file " + s);
                        }
                    }
                }
                if (hdrdelimidx < 0)
                {
                    if (Settings.OptAutoAddEndOfHeaderComment)
                    {
                        // add the end of header comment in the beginning of the file
                        lines = lines.InsertAt(Settings.HeaderDelimiter, 0);
                    }
                    else
                    {
                        // throw exception and stop because we cannot really continue
                        throw new Exception("File " + s + " does not have a header delimiter.");
                    }
                }

                // now check for end of file marker
                int lastline = lines.Count();
                int j = lastline - 1;
                bool LastLineCommentFound = false;
                bool FileEndsInCRLF = false;
                EndOfFileStates State = EndOfFileStates.LookForCRLF;

                while ((j >= 0) && (State != EndOfFileStates.Done))
                {
                    switch (State)
                    {
                        case EndOfFileStates.LookForCRLF:
                            if (lines[j] == "")
                            {
                                // last line contains only CRLF
                                FileEndsInCRLF = true;
                                State = EndOfFileStates.LookForEndofLineComment;
                                j--;
                            }
                            else
                            {
                                State = EndOfFileStates.Done;
                            }
                            break;

                        case EndOfFileStates.LookForEndofLineComment:
                            if (lines[j] != "")
                            {
                                if (lines[j].Contains(Settings.LastLine))
                                {
                                    // "last line" comment found - now we can carry on
                                    LastLineCommentFound = true;
                                    State = EndOfFileStates.Done;
                                }
                            }
                            j--;
                            break;

                        case EndOfFileStates.Done:
                            break;
                    }
                }

                if (!FileEndsInCRLF)
                {
                    string llerr = "Last line(s) do not contain CR-LF";
                    if (Settings.OptVerbose)
                    {
                        Console.WriteLine("L" + (j - 2).ToString() + " " + lines[j - 2]);
                        Console.WriteLine("L" + (j - 1).ToString() + " " + lines[j - 1]);
                        Console.WriteLine("L" + j.ToString() + " " + lines[j]);
                        Console.WriteLine("Error: " + llerr);
                    }
                    Errors.Add(llerr);
                }

                if (!LastLineCommentFound)
                {
                    if (Settings.OptAutoAddLastLineComment)
                    {
                        lines = lines.Append(Environment.NewLine);
                        lines = lines.Append(Settings.LastLine + Environment.NewLine);
                        lines = lines.Append(Environment.NewLine);
                    }
                    else
                    {
                        string llnotfounderr = "Last line comment not found";
                        Errors.Add(llnotfounderr);
                    }
                }

                if (Errors.Count() > 0)
                {
                    StringBuilder errstr = new StringBuilder(Errors.Count() * 160);

                    foreach(string Forbiddenerrstr in Errors)
                    {
                        errstr.AppendLine(Forbiddenerrstr);
                    }
                    throw new Exception("Problems found: " + Environment.NewLine + errstr.ToString());
                }

                PrepareHeader(ref headerlines, s, timestamp);

                // now write the temp file
                if (Settings.OptVerbose) Console.WriteLine("Now writing output file");
                string[] outfile = new string[headerlines.Count() + lines.Count() - hdrdelimidx];
                int k = 0;
                int m = 0;

                k = 0;
                foreach (string h in headerlines)
                {
                    if (Settings.OptVerbose) Console.WriteLine("Add Line" + h);
                    outfile[k] = h;
                    k++;
                }
                m = 0;
                foreach (string l in lines)
                {
                    if (m >= hdrdelimidx)
                    {
                        if (Settings.OptVerbose) Console.WriteLine("Add Line" + l);
                        outfile[k] = l;
                        k++;
                    }
                    m++;
                }
                File.WriteAllLines(s + ".autotemp",outfile);
                File.Copy(s + ".autotemp", s, true);
                File.SetLastWriteTime(s, timestamp);
                File.Delete(s + ".autotemp");

            }
            catch (Exception exc)
            {
                throw exc;
            }
        }

        static bool FileMatchesExcludeList(string file)
        {
            try
            {
                bool result = false;
                foreach (string s in Settings.ExcludeList)
                {
                    if (file.Contains(s))
                    {
                        result = true;
                    }
                }
                return result;
            }
            catch(Exception exc)
            {
                throw exc;
            }
        }

        static void DoAllFiles()
        {
            try
            {
                List<string> IgnoreList = new List<string>();
                string[] files = new [] { "" };
                List<string> ProcessList;

                foreach (string filepattern in Settings.FilesList)
                {
                    files = Directory.GetFiles(Settings.SourceDirectory, filepattern, SearchOption.AllDirectories);
                    ProcessList = new List<string>(files);

                    foreach (string ignorepattern in Settings.ExcludeList)
                    {
                        string[] ignorelistarr = PatternMatch.FindMatchingFiles(ignorepattern, files);

                        foreach(string ignorefile in ignorelistarr)
                        {
                            IgnoreList.Add(ignorefile);
                        }
                    }

                    // now process files
                    foreach (string file in ProcessList)
                    {
                        if (IgnoreList.Contains(file) || FileMatchesExcludeList(file))
                        {
                            Console.WriteLine("Skipping file: " + file);
                        }
                        else
                        {
                            ProcessFile(file);
                            Console.ReadLine();
                        }
                    }
                }

            }
            catch (Exception exc)
            {
                throw new Exception("Error in scanning through files" + Environment.NewLine, exc);
            }
        }

        static string GetSettingsFile()
        {
            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "autosourceheadersettings.xml";
        }

        private static void LoadSettings()
        {
            string f = GetSettingsFile();
            Settings.Clear();
            Xml<AppSettings>.LoadFromXml(ref Settings, f);
        }

        static string ExceptionString(Exception ex)
        {
            if (Settings.OptLongErrors)
            {
                return ex.ToString();
            }
            else
            {
                StringBuilder str = new StringBuilder(2048);
                Exception inner = ex;
                while(inner != null)
                {
                    str.Append(inner.Message);
                    inner = inner.InnerException;
                }
                string s = str.ToString();

                return s;
            }
        }

        static void Main(string[] args)
        {
            int i;

            if (Commandline.FindParam("--help") > 0)
            {
                Console.WriteLine("  autosourceheader.exe help:"+Environment.NewLine);
                Console.WriteLine("    --listargs       - List program arguments and exit (for debugging only)");
                Console.WriteLine("    --savecfg        - Save an example autosourceheadersettings.xml file for editing of settings.");
                Console.WriteLine("    --checktodo      - Do checking for uncompleted TODOs.");
                Console.WriteLine("    --nochecktodo    - Skip checking for uncompleted TODOs.");
                Console.WriteLine("    --verbose        - Show extra detailed information.");
                Console.WriteLine("    --noverbose      - Do NOT show extra detailed information.");
                Console.WriteLine("    --checkascii     - Ensure that the file contains ASCII characters only");
                Console.WriteLine("    --nocheckascii   - Skip ASCII characters checking");
                Console.WriteLine("    --longerrors     - Show long erros (full stack trace)");
                Console.WriteLine("");
                Console.WriteLine("    --auto-add-last-line-comment       - Automatically add the last line comment.");
                Console.WriteLine("    --auto-add-end-of-header-comment   - Automatically addd the end of header comment.");
                return;
            }
            LoadSettings();

            if (Commandline.FindParam("--listargs") > 0)
            {
                i = 0;
                foreach (string s in args)
                {
                    Console.WriteLine("Argument {0} = {1}", i, s);
                    i++;
                }
                return;
            }
            if (Commandline.FindParam("--nochecktodo") > 0)
            {
                Settings.OptCheckTODOs = false;
            }
            if (Commandline.FindParam("--checkascii") > 0)
            {
                Settings.OptCheckASCII = false;
            }
            if (Commandline.FindParam("--verbose") > 0)
            {
                Settings.OptVerbose = true;
            }
            if (Commandline.FindParam("--longerrors") > 0)
            {
                Settings.OptLongErrors = true;
            }
            if (Commandline.FindParam("--auto-add-last-line-comment") > 0)
            {
                Settings.OptAutoAddLastLineComment = true;
            }
            if (Commandline.FindParam("--auto-add-end-of-header-comment") > 0)
            {
                Settings.OptAutoAddEndOfHeaderComment = true;
            }

            if (Commandline.FindParam("--savecfg") > 0)
            {
                try
                {
                    string f = GetSettingsFile();
                    Xml<AppSettings>.SaveToXml(Settings, f);
                    NeoConsole.WriteLineSuccess("Config saved to file "+ f);
                    return;
                }
                catch(Exception exc)
                {
                    NeoConsole.WriteLineError("Error occured: " + ExceptionString(exc));
                }
            }

            if (Settings.OptVerbose)
            {
                Console.WriteLine("Settings file name used: "+GetSettingsFile());

                Console.WriteLine("Settings:");
                Console.WriteLine(Settings.ToString());

                string jsonText = JsonConvert.SerializeObject(Settings, Formatting.Indented);

                Console.WriteLine("JSON: "+jsonText);

                // ReflectionUtils.GetClassProperties(Settings);
            }
            try
            {
                DoAllFiles();
            }
            catch(Exception exc)
            {
                NeoConsole.WriteLineError("Error occured: " +ExceptionString(exc));
            }
        }
    }
}
