using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Avigero.Helper;

namespace Avigero.Dataset
{
    class TimeSeries
    {
        private static List<DateTime> CreationDates;
        private static DateTime StartDate;
        private static int[] BugsPerDay;

        public TimeSeries()
        {
            CreationDates = new List<DateTime>();
            m_Project = string.Empty;
        }

        private string m_Project; 
        public string Project
        {
            set { m_Project = value; }
        }

        public void AddValue(DateTime creationDate)
        {
            CreationDates.Add(creationDate);
        }

        public void Create()
        {
            // First step: create the data array
            CreationDates.Sort();
            int numDays = System.Convert.ToInt32((CreationDates[CreationDates.Count-1] - CreationDates[0]).TotalDays)+1;
            // Create an array, size is number of days
            BugsPerDay = new int[numDays];
            // Set the start date
            StartDate = CreationDates[0];
            // Also need a temprorary date
            DateTime td = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day);

            int i = 0;
            while (i < numDays)
            {
                // Get the number of bugs created on this day and add it to the array
                BugsPerDay[i] = CreationDates.Count(mydate => (mydate.Year == td.Year
                                                        && mydate.Month == td.Month
                                                        && mydate.Day == td.Day));
                // Proceed with next day
                td = td.AddDays(1);
                i++;
            }

            // Now really create the time series diagram

            // Read the template file
            TextReader FileReader = null;
            FileReader = File.OpenText("../Templates/" + TemplateFiles.TimeSeriesDiagram);
            string Content = FileReader.ReadToEnd();
            FileReader.Close();

            // DataString
            string dataString = string.Empty;
            foreach (int value in BugsPerDay)
            {
                dataString += value + ", ";
            }
            // Remove last ", "
            char[] trimEnd = { ',', ' ' };
            dataString = dataString.TrimEnd(trimEnd);

            // Replace content in output file
            Content = Content.Replace(TemplateTags.tags[(int)TemplateTagNames.TimeSeriesData], dataString);
            Content = Content.Replace(TemplateTags.tags[(int)TemplateTagNames.ProjectName], m_Project);

            // Start Year, Month, Day
            Content = Content.Replace(TemplateTags.tags[(int)TemplateTagNames.StartYear], StartDate.Year.ToString());
            // -1 is the stupid highcharts counting system
            Content = Content.Replace(TemplateTags.tags[(int)TemplateTagNames.StartMonth], (StartDate.Month-1).ToString());
            Content = Content.Replace(TemplateTags.tags[(int)TemplateTagNames.StartDay], StartDate.Day.ToString());

            // Write the output file
            // Write Output File
            TextWriter timeSeriesText = new StreamWriter("../Output/TimeSeries/" + m_Project + ".htm");
            timeSeriesText.Write(Content);
            timeSeriesText.Close();
        }
    }
}
