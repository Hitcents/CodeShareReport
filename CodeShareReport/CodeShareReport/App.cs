using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeShareReport
{
    class App
    {
        static void Main(string[] args)
        {
            var projects = new List<Solution> {

                new Solution {
                    Name = "iOS",
                    ProjectFiles = new List<string> {
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\iOS\DrawAStickman.Core\DrawAStickman.Core.csproj",
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\iOS\DrawAStickman.iPhone\DrawAStickman.iPhone.csproj",
                    },

                    IgnoreList = new List<Regex> {
                        new Regex("AssemblyInfo\\.cs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                    },
                },

                new Solution {
                    Name = "Android",
                    ProjectFiles = new List<string> {
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\Droid\DrawAStickman.Core\DrawAStickman.Core.csproj",
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\Droid\DrawAStickman.Droid\DrawAStickman.Droid.csproj",
                    },

                    IgnoreList = new List<Regex> {
                        //Remove additional code files supporting IAPs on various app stores, only count Google Play IAPs
                        new Regex("SAMSUNG", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                        new Regex("AMAZON", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                        new Regex("INAPPPURCHASING", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                        new Regex("Resource\\.designer\\.cs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                        new Regex("AssemblyInfo\\.cs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                    },
                },

                new Solution {
                    Name = "WinRT",
                    ProjectFiles = new List<string> {
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\Windows8\DrawAStickman.Core\DrawAStickman.Core.csproj",
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\Windows8\DrawAStickman.Windows8.Xaml\DrawAStickman.Windows8.csproj",
                    },

                    IgnoreList = new List<Regex> {
                        new Regex("AssemblyInfo\\.cs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                    },
                },

                new Solution {
                    Name = "WinPho",
                    ProjectFiles = new List<string> {
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\WindowsPhone\DrawAStickman.Core.WP7\DrawAStickman.Core.WP7.csproj",
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\WindowsPhone\DrawAStickman.WP7\DrawAStickman.WP7\DrawAStickman.WP7.csproj",
                    },

                    IgnoreList = new List<Regex> {
                        //This is a zip library for doing GZip compression, needed on WP7 only
                        new Regex("SHARPZIPLIB", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                        new Regex("AssemblyInfo\\.cs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                    },
                },

                new Solution {
                    Name = "Mac",
                    ProjectFiles = new List<string> {
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\Mac\DrawAStickman.Mac\DrawAStickman.Core\DrawAStickman.Core.csproj",
                        @"Y:\Desktop\MonoTouch\DrawAStickman\Code\Mac\DrawAStickman.Mac\DrawAStickman.Mac\DrawAStickman.Mac.csproj",
                    },

                    IgnoreList = new List<Regex> {
                        new Regex("AssemblyInfo\\.cs", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                    },
                },

            };

            new App().Run(projects);

            Console.WriteLine("Press enter to quit...");
            Console.ReadLine();
        }

        class Solution
        {
            public string Name = "";
            public List<string> ProjectFiles = new List<string>();
            public List<FileInfo> CodeFiles = new List<FileInfo>();
            public List<Regex> IgnoreList = new List<Regex>();
            public override string ToString()
            {
                return Name;
            }

            public int UniqueLinesOfCode
            {
                get
                {
                    return (from f in CodeFiles
                            where f.Solutions.Count == 1
                            select f.LinesOfCode).Sum();
                }
            }

            public int SharedLinesOfCode
            {
                get
                {
                    return (from f in CodeFiles
                            where f.Solutions.Count > 1
                            select f.LinesOfCode).Sum();
                }
            }

            public int TotalLinesOfCode
            {
                get
                {
                    return (from f in CodeFiles
                            select f.LinesOfCode).Sum();
                }
            }
        }

        class FileInfo
        {
            public string Path = "";
            public List<Solution> Solutions = new List<Solution>();
            public int LinesOfCode = 0;
            public override string ToString()
            {
                return Path;
            }
        }

        Dictionary<string, FileInfo> _files = new Dictionary<string, FileInfo>();

        void AddRef(string path, Solution sln)
        {
            path = path.ToUpperInvariant();

            if (_files.ContainsKey(path))
            {
                _files[path].Solutions.Add(sln);
                sln.CodeFiles.Add(_files[path]);
            }
            else if (sln.IgnoreList == null || !sln.IgnoreList.Select(i => i.IsMatch(path)).FirstOrDefault(t => t))
            {
                var info = new FileInfo { Path = path, };
                info.Solutions.Add(sln);
                _files[path] = info;
                sln.CodeFiles.Add(info);
            }
        }

        void Run(List<Solution> solutions)
        {
            //
            // Find all the files
            //
            foreach (var sln in solutions)
            {
                foreach (var projectFile in sln.ProjectFiles)
                {
                    var dir = Path.GetDirectoryName(projectFile);
                    var projectName = Path.GetFileNameWithoutExtension(projectFile);
                    var doc = XDocument.Load(projectFile);
                    var q = from x in doc.Descendants()
                            let e = x as XElement
                            where e != null
                            where e.Name.LocalName == "Compile"
                            where e.Attributes().Any(a => a.Name.LocalName == "Include")
                            select e.Attribute("Include").Value;
                    foreach (var inc in q)
                    {
                        AddRef(Path.GetFullPath(Path.Combine(dir, inc)), sln);
                    }
                }
            }

            //
            // Get the lines of code
            //
            foreach (var f in _files.Values)
            {
                try
                {
                    f.LinesOfCode = File.ReadAllLines(f.Path).Length;
                }
                catch (Exception)
                {
                }
            }

            //
            // Output
            //
            Console.WriteLine("app\tt\tu\ts\tu%\ts%");
            foreach (var sln in solutions)
            {
                Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4:p}\t{5:p}",
                    sln.Name,
                    sln.TotalLinesOfCode,
                    sln.UniqueLinesOfCode,
                    sln.SharedLinesOfCode,
                    sln.UniqueLinesOfCode / (double)sln.TotalLinesOfCode,
                    sln.SharedLinesOfCode / (double)sln.TotalLinesOfCode);
            }

            Console.WriteLine("DONE");
        }
    }
}