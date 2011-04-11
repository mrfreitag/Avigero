using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace FolderStats
{
    class FolderStats
    {
        /// <summary>
        /// Create a summary overview without deserializing any of the files.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string inputfolder = "../BugzillaDBs/";
            string outputfolder = "../Output/Folderstats/";
            string templatefolder = "../Templates/";
            string invalid = "/Invalid/";
            string sorted = "/Sorted/";
            string unsorted = "/Unsorted/";

            List<Project> projects = new List<Project>();

            // Check if output folder exists;
            if (!Directory.Exists(inputfolder) || !Directory.Exists(templatefolder))
            {
                Console.WriteLine("Input or template folder does not exist. Bye.");
                return;
            }
            if (!Directory.Exists(outputfolder))
                Directory.CreateDirectory(outputfolder);

            // Get the list of projects
            foreach(string folder in Directory.GetDirectories(inputfolder))
            {
                Project project = new Project();
                project.Name = folder.Remove(0, inputfolder.Length);
                // Get number of invalid, sorted, and unsorted bugs
                string[] files;

                files = Directory.GetFiles(folder + unsorted);
                project.NumUnsorted = files.Length;
                files = Directory.GetFiles(folder + invalid);
                project.NumInvalid = files.Length;
                files = Directory.GetFiles(folder + sorted);
                project.NumSorted = files.Length;
                // Total Number of bugs
                project.NumBugs = project.NumSorted + project.NumUnsorted + project.NumInvalid;

                projects.Add(project);
            }

            CreateProjectOverview(projects);
        }

        private static void CreateProjectOverview(List<Project> projects)
        {
            Console.WriteLine("Listing project statistics");
            foreach (Project project in projects)
            {
                Console.WriteLine(project.Name);
                Console.WriteLine("\tSorted: {0}", project.NumSorted);
                Console.WriteLine("\tUnsorted: {0}", project.NumUnsorted);
                Console.WriteLine("\tInvalid: {0}", project.NumInvalid);
                Console.WriteLine("\tTotal: {0}", project.NumBugs);
            }
        }

        struct Project
        {
            public string Name;
            public int NumBugs;
            public int NumInvalid;
            public int NumSorted;
            public int NumUnsorted;
            public int NumPackages;
            public int NumComponents;

        }
    }
}
