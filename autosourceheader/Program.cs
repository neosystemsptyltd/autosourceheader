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
        public List<string> FilesList;

        /// <summary>
        /// List of files to exclude
        /// </summary>
        public List<string> ExcludeList;

        /// <summary>
        /// List of opensource notices
        /// </summary>
        public List<OpenSourceNoticeFile> OpenSourceNotices;

        /// <summary>
        /// List of files to exclude
        /// </summary>
        public string SourceDirectory;

        /// <summary>
        /// List of forbidden words in the code
        /// </summary>
        public List<string> ForbiddenWords;

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
        /// Constructor
        /// </summary>
        public AppSettings()
        {
            ProjectName = "FSK Board reprogramming project";
            Compiler = "Atmel Studio 7.0, GCC AVR 4.9.2";
            Author = "A Jordaan";
            Description = "Source files for FSK Board reprogramming project";
            Copyright = "(c) 2015 Radio Data Communications"+Environment.NewLine + "// (Some file in this project are open source)";

            HeaderFilename = "autosourceheader.txt";
            HeaderDelimiter = "/// End of Header";
            LastLine = "/// End of file";
            SourceDirectory = @"C:\Users\armand\ownCloud\Projects\RDC\FSKProject\Design\Software\Firmware\FSK_GSM_Unit\FSK_GSM_Unit\src";

            FilesList = new List<string>();
            FilesList.Add("*.cpp");
            FilesList.Add("*.c");
            FilesList.Add("*.h");

            ExcludeList = new List<string>();
            ExcludeList.Add("pthread" + Path.DirectorySeparatorChar);
            ExcludeList.Add("ext" + Path.DirectorySeparatorChar);

            OpenSourceNotices = new List<OpenSourceNoticeFile>();

            ForbiddenWords = new List<string>();
            ForbiddenWords.Add("Netstar");
            ForbiddenWords.Add("Reality");
            ForbiddenWords.Add("OEM");
            ForbiddenWords.Add("Duotronic");
            ForbiddenWords.Add("Sema");
            ForbiddenWords.Add("Tsalano");
            ForbiddenWords.Add("???");
        }
    }

    class Program
    {
        private static AppSettings Settings = new AppSettings();

        private static bool OptCheckTODOs = true;

        private static bool OptVerbose = false;

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
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Error in preparing Header file", exc);
            }
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
                    if (OptCheckTODOs)
                    {
                        if (lines[i].Contains("TODO",StringComparison.OrdinalIgnoreCase))
                        {
                            Errors.Add("TODO found at line " + linecount.ToString() + " in file " + s);
                        }
                    }
                }
                if (hdrdelimidx < 0)
                {
                    // throw exception and stop because we cannot really continue
                    throw new Exception("File " + s + " does not have a header delimiter.");
                }

                // now check for end of file marker
                int lastline = lines.Count();
                int j = lastline - 1;
                bool LastLineCommentFound = false;
                bool LastLineCommentFatalErr = false;

                while((j >= 0) && !LastLineCommentFound && !LastLineCommentFatalErr)
                {
                    if (lines[j] == "")
                    {
                        // quitely absorb
                    }
                    else if (lines[j].Contains(Settings.LastLine))
                    {
                        // "last line" comment found - now we can carry on
                        LastLineCommentFound = true;
                    }
                    else
                    {
                        string llerr = "Last line(s) do not contain CR-LF";
                        if (OptVerbose)
                        {
                            Console.WriteLine("L" + (j - 2).ToString() + " " + lines[j - 2]);
                            Console.WriteLine("L" + (j - 1).ToString() + " " + lines[j - 1]);
                            Console.WriteLine("L" + j.ToString() + " " + lines[j]);
                            Console.WriteLine("Error: " + llerr);
                        }
                        Errors.Add(llerr);
                        LastLineCommentFatalErr = true;
                    }
                    j--;
                }

                if ( j < 0 )
                {
                    string llnotfounderr = "Last line comment not found";
                    Errors.Add(llnotfounderr);
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
                if (OptVerbose) Console.WriteLine("Now writing output file");
                string[] outfile = new string[headerlines.Count() + lines.Count() - hdrdelimidx];
                int k = 0;
                int m = 0;

                k = 0;
                foreach (string h in headerlines)
                {
                    if (OptVerbose) Console.WriteLine("Add Line" + h);
                    outfile[k] = h;
                    k++;
                }
                m = 0;
                foreach (string l in lines)
                {
                    if (m >= hdrdelimidx)
                    {
                        if (OptVerbose) Console.WriteLine("Add Line" + l);
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
                        }
                    }
                }

            }
            catch (Exception exc)
            {
                throw new Exception("Error in scanning through files" + Environment.NewLine, exc);
            }
        }

        static void Main(string[] args)
        {
            int i;

            if (Commandline.FindParam("--help") > 0)
            {
                Console.WriteLine("  autosourceheader.exe help:"+Environment.NewLine);
                Console.WriteLine("    --listargs    - List program arguments and exit (for debugging only)");
                Console.WriteLine("    --savecfg     - Save an example autosourceheadersettings.xml file for editing of settings.");
                Console.WriteLine("    --nochecktodo - Skip checking for uncompleted TODOs.");
                Console.WriteLine("    --verbose     - Show extra detailed information.");
                return;
            }
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
                OptCheckTODOs = false;
            }
            if (Commandline.FindParam("--verbose") > 0)
            {
                OptVerbose = true;
            }
            if (Commandline.FindParam("--savecfg") > 0)
            {
                try
                {
                    string f = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "autosourceheadersettings.xml";
                    Xml<AppSettings>.SaveToXml(Settings, f);
                    NeoConsole.WriteLineSuccess("Config saved to file "+ f);
                    return;
                }
                catch(Exception exc)
                {
                    NeoConsole.WriteLineError("Error occured: " + exc.ToString());
                }
            }

            try
            {
                DoAllFiles();
            }
            catch(Exception exc)
            {
                NeoConsole.WriteLineError("Error occured: " + exc.ToString());
            }
        }
    }
}
