using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Avigero.Helper;

namespace Avigero.Dataset
{
    public struct DatePoint
    {
        public DateTime start, stop;
        public TimeSpan ts;

        public DatePoint(DateTime p1, DateTime p2)
        {
            start = p1;
            stop = p2;
            ts = p2 - p1;
        }
    }
    
    class ScatterPlot
    {
        private static List<DatePoint> DatePoints;

        private double m_Average;
        private double m_Median;
        private double m_LowQuartile;
        private double m_HighQuartile;

        // statistics
        public double Average { get { return m_Average; } }
        public double Median { get { return m_Median; } }
        public double LowQuartile { get { return m_LowQuartile; } }
        public double HighQuartile { get { return m_HighQuartile; } }

        public ScatterPlot()
        {
            DatePoints = new List<DatePoint>();
            m_Project = string.Empty;
        }

        private string m_Project; 
        public string Project
        {
            set { m_Project = value; }
        }

        public void AddValue(DateTime creation_ts, DateTime delta_ts)
        {
            DatePoint ts = new DatePoint(creation_ts, delta_ts);
            DatePoints.Add(ts);
        }

        public void CreateStatistics()
        {
            List<double> daysToFix = DatePoints.Select(x => x.ts.TotalDays).ToList();
            m_Average = daysToFix.Average();

            // Sort the DatePoints
            //m_RawData.Sort( delegate(KeyValuePair<DateTime, double> x, KeyValuePair<DateTime, double> y) {return x.Key.CompareTo(y.Key);} );
            DatePoints.Sort(delegate(DatePoint x, DatePoint y) { return x.ts.CompareTo(y.ts); });
        }

        public void Create()
        {
            // Create the datastring that will be put into the HTML
            DateTime dt1970 = new DateTime(1970, 1, 1);
            string dataString = string.Empty;
            foreach (DatePoint ts in DatePoints)
            {
                // The result has to look like this:
                // [ Date.UTC(2006,0,1), Date.UTC(2006,0,2)], [Date.UTC(2006,0,7), xx.xx]

                //dataString += "[Date.UTC(" + ts.start.Year + ","
                //                           + (ts.start.Month - 1) + ","
                //                           + ts.start.Day + ")," 
                //                           + ts.ts.TotalDays.ToString("0.##")
                //                           + "], ";
                TimeSpan utc = ts.start - dt1970;

                dataString += "[" + utc.TotalMilliseconds + ","
                                               + ts.ts.TotalDays.ToString("0.##")
                                               + "], ";
            }
            // Remove last ", "
            char[] trimEnd = { ',', ' ' };
            dataString = dataString.TrimEnd(trimEnd);

            // Now really create the time series diagram

            // Read the template file
            TextReader FileReader = null;
            FileReader = File.OpenText("../Templates/" + TemplateFiles.ScatterPlot);
            string Content = FileReader.ReadToEnd();
            FileReader.Close();

            // Replace content in output file
            Content = Content.Replace(TemplateTags.tags[(int)TemplateTagNames.ScatterPlotData], dataString);
            Content = Content.Replace(TemplateTags.tags[(int)TemplateTagNames.ProjectName], m_Project);

            // Write the output file
            // Write Output File
            TextWriter timeSeriesText = new StreamWriter("../Output/ScatterPlot/" + m_Project + ".htm");
            timeSeriesText.Write(Content);
            timeSeriesText.Close();
        }
    }
}
