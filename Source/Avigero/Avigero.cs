using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using Avigero.Dataset;
using System.Collections;
using Avigero.Entities;
using Avigero.Helper;

namespace Avigero
{
    class Avigero
    {
        static bool splitBugs = true;
        static bool doTheMeat = true;
        static bool packagesAndComponents = true;

        private static string inputFolder;

        private static int stat_filesAnalyzed;
        private static int stat_filesTried;
        private static int stat_fixedAndResolved;

        static void Main(string[] args)
        {
            // Preparation phase
            stat_filesAnalyzed = 0;
            stat_filesTried = 0;
            stat_fixedAndResolved = 0;
            bool demo = false;

            if (demo)
            {
                inputFolder = @"..\BugzillaDBsDemo\";
            }
            else
            {
                inputFolder = @"..\BugzillaDBs\";
            }
            // Check if all input folders and all output folders exist
            CheckInputOutputFolders();

            // Step 1: Usually there are a lot of bugs and it makes sense to split the input into several subfolders
            // first. The structure is Project, Product, Components. Project is the name of the input folder, Product
            // and Component are both defined in the XSD of Bugzilla.

            // will be implemented later tbd

            // Step 2: Create a time series diagram of the raw data.
            //string[] folders = new string[] {"Kernel1k"};
            string[] folders;
            folders = Directory.GetDirectories(inputFolder);
            //folders = new string[] { 
                //inputFolder + "Eclipse",
                //inputFolder + "Gentoo",
                //inputFolder + "Gnome",
                //inputFolder + "KDE",
                //inputFolder + "Kernel",
                //inputFolder + "Mandriva",
                //inputFolder + "Mozilla",
            //    inputFolder + "Redhat"};
            //string[] folders = new string[] { "Apache" };

            if (packagesAndComponents)
            {
                ExtractPackagesAndComponents(folders);
            }

            if (doTheMeat)
            {
                // Each folder is equivalent to a project
                foreach (string projectfolder in folders)
                {
                    // Each project needs a DataSuperset
                    DataSuperset projectSuperset = new DataSuperset();

                    projectSuperset.Project = GetProjectName(projectfolder);

                    Console.WriteLine("Entering Directory {0}", projectfolder);
                    // Set the project name
                    TimeSeries TimeSeriesDiagram = new TimeSeries();
                    TimeSeriesDiagram.Project = GetProjectName(projectfolder);
                    projectSuperset.Name = GetProjectName(projectfolder);

                    ScatterPlot ScatterPlotDiagram = new ScatterPlot();
                    ScatterPlotDiagram.Project = projectfolder.Substring(projectfolder.LastIndexOf(@"\") + 1);

                    // Initialize the global overview
                    GlobalOverview globalOverview = new GlobalOverview();
                    globalOverview.Initialize();
                    globalOverview.Project = GetProjectName(projectfolder);


                    // Create the quarantine folder
                    if (!Directory.Exists(projectfolder + @"\Unsorted"))
                        Directory.CreateDirectory(projectfolder + @"\Unsorted");

                    // Blockfolders
                    string blocksfolder = projectfolder + @"\Blocks\";
                    foreach (string blockfolder in Directory.GetDirectories(blocksfolder))
                    {

                        foreach (string filename in Directory.GetFiles(blockfolder))
                        {
                            FileInfo myFile = new FileInfo(filename);
                            try
                            {
                                stat_filesTried++;
                                TextReader fileReader = new StreamReader(myFile.FullName, Encoding.UTF8);
                                XmlSerializer rqSerializer = new XmlSerializer(typeof(bugzilla));
                                bugzilla BugzillaBug;
                                BugzillaBug = (bugzilla)rqSerializer.Deserialize(fileReader);
                                stat_filesAnalyzed++;
                                bug Bug = BugzillaBug.bug[0];
                                // We have the bug, add it to the superset
                                projectSuperset.RawData.Add(Bug);

                                string product = Bug.product.Text[0];
                                string component = Bug.component.Text[0];

                                projectSuperset.AddProductComponent(product, component);

                                // Console.WriteLine("{0} - {1}", product, component);

                                // First thing for the visualization: Time Series Diagram of the raw data
                                // At least Gentoo has a format that C# does not like. Therefore only the first
                                // 16 digits will be tried to convert in case the initial one fails
                                // yyyy-MM-dd hh:mm
                                DateTime creationDate;
                                if (!DateTime.TryParse(Bug.creation_ts.Text[0], out creationDate))
                                    DateTime.TryParse(Bug.creation_ts.Text[0].Substring(0, 16), out creationDate);

                                DateTime closingDate;
                                if (!DateTime.TryParse(Bug.delta_ts.Text[0], out closingDate))
                                    DateTime.TryParse(Bug.delta_ts.Text[0].Substring(0, 16), out closingDate);

                                //DateTime creationDate = System.Convert.ToDateTime(Bug.creation_ts.Text[0]);


                                // Only consider bugs created after 1980, everything else is considered to be wrong.
                                if (creationDate.Year > 1980)
                                {
                                    // First treat the global overview
                                    globalOverview.AddProduct(Bug.product.Text[0]);
                                    globalOverview.AddComponent(Bug.component.Text[0]);

                                    TimeSeriesDiagram.AddValue(creationDate);
                                    // Only add the ones that have been resolved and fixed
                                    if (Bug.bug_status.Text[0].Equals("RESOLVED") && Bug.resolution.Text[0].Equals("FIXED"))
                                    {
                                        ScatterPlotDiagram.AddValue(creationDate, closingDate);
                                        stat_fixedAndResolved++;
                                    }
                                }
                                else
                                    Console.WriteLine("Error in bug {0}, created in {1}", filename, creationDate.Year);
                            }
                            catch (System.IO.IOException)
                            {
                                // In case of an error, move the file to the unsorted folder
                                Console.WriteLine("IOException; Move {0}", myFile.Name);
                                //File.Move(myFile.FullName, inputFolder + @"\Unsorted\" + myFile.Name);
                                continue;
                            }
                            catch (Exception e)
                            {
                                // In case of an error, move the file to the unsorted folder
                                //Console.WriteLine("Exception ({0}); Move {1}", e.Message, myFile.Name);
                                //File.Move(myFile.FullName, inputFolder + @"\Unsorted\" + myFile.Name);
                                // just skip
                                continue;
                            }
                        }
                    }

                    //

                    // All values have been added, so now it is time to create the diagram
                    TimeSeriesDiagram.Create();
                    ScatterPlotDiagram.Create();

                    // Write statistics
                    Console.WriteLine("Tried {0} files", stat_filesTried);
                    Console.WriteLine("Analyzed {0} files", stat_filesAnalyzed);
                    Console.WriteLine("Fixed and Resolved: {0} files", stat_fixedAndResolved);

                    // This is where we do the meat ;-)
                    projectSuperset.Evaluate();
                    projectSuperset.CreatePredictionDiagrams();
                    projectSuperset.ListStatistics();

                    // Reset counters
                    stat_filesAnalyzed = 0;
                    stat_filesTried = 0;
                    stat_fixedAndResolved = 0;
                }
            }
        }

        private static void CheckInputOutputFolders()
        {
            if (!Directory.Exists(OutputDirectories.OutputDirectory))
                Directory.CreateDirectory(OutputDirectories.OutputDirectory);

            if (!Directory.Exists(OutputDirectories.PieDonutCharts))
                Directory.CreateDirectory(OutputDirectories.PieDonutCharts);

            if (!Directory.Exists(OutputDirectories.TimeSeriesDiagrams))
                Directory.CreateDirectory(OutputDirectories.TimeSeriesDiagrams);

            if (!Directory.Exists(OutputDirectories.ScatterPlots))
                Directory.CreateDirectory(OutputDirectories.ScatterPlots);

            if (!Directory.Exists(OutputDirectories.MachineLearningOutput))
                Directory.CreateDirectory(OutputDirectories.MachineLearningOutput);
        }

        /// <summary>
        /// Return the name of the folder which is the project name.
        /// </summary>
        /// <param name="folder">String as returned by Directory.GetDirectories</param>
        /// <returns></returns>
        private static string GetProjectName(string folder)
        {
            return folder.Substring(folder.LastIndexOf(@"\") + 1);
        }

        private static void ExtractPackagesAndComponents(string[] folders)
        {
            // Projects
            foreach (string projectfolder in folders)
            {
                string projectName = GetProjectName(projectfolder);
                List<KeyValuePair<string, int>> ProjectWeights = new List<KeyValuePair<string, int>>();
                int numPackages = 0;
                // Packages
                foreach (string packagefolder in Directory.GetDirectories(projectfolder + @"\Packages\"))
                {
                    numPackages++;
                    string packageName = GetProjectName(packagefolder);
                    int numPackageBugs = 0;
                    // Components
                    foreach (string componentfolder in Directory.GetDirectories(packagefolder))
                    {
                        // Files
                        string[] bugFiles = Directory.GetFiles(componentfolder);
                        int numComponentBugs = bugFiles.Length;
                        numPackageBugs += numComponentBugs;
                    }
                    ProjectWeights.Add(new KeyValuePair<string,int>(packageName, numPackageBugs));
                }
                // Project is done, now create the diagram
                PieDonut donutChart = new PieDonut();
                donutChart.Project = projectName;
                donutChart.ProjectWeights = ProjectWeights;
                donutChart.Create();
            }
        }
    }
}