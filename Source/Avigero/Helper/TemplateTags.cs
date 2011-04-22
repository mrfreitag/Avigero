using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avigero.Helper
{
    internal enum TemplateTagNames
    {
        TimeSeriesData = 0,
        ScatterPlotData,
        ProjectName,
        ProductName,
        ComponentName,
        StartYear,
        StartMonth,
        StartDay,
        Xmax,
        Ymax,
        PieDataEntries
    }

    internal class TemplateTags
    {
        public const int TAG_COUNT = 11;
        public static string[] tags = new string[TAG_COUNT]{
            "($TimeSeriesData)",
            "($ScatterPlotData)",
            "($ProjectName)",
            "($ProductName)",
            "($ComponentName)",
            "($StartYear)",
            "($StartMonth)",
            "($StartDay)",
            "($Xmax)",
            "($Ymax)",
            "($PieDataEntries)"
        };
    }

    internal class TemplateFiles
    {
        public static string TimeSeriesDiagram = "line-time-series.htm";
        public static string ScatterPlot = "scatter.htm";
        public static string PieDonut = "pie-donut.htm";
    }

    internal class BugStatus
    {
        public static string Status_New = "NEW";
        public static string Status_Assigned = "ASSIGNED";
        public static string Status_Unconfirmed = "UNCONFIRMED";
        public static string Status_Reopened = "REOPENED";
        public static string Status_Resolved = "RESOLVED";
    }

    internal class OutputDirectories
    {
        public static string OutputDirectory = "../Output";
        public static string PieDonutCharts = "../Output/PieDonuts";
        public static string TimeSeriesDiagrams = ("../Output/TimeSeries");
        public static string ScatterPlots = ("../Output/ScatterPlot");
        public static string MachineLearningOutput = ("../Output/MachineLearningOutput");
    }

    internal class Severities
    {
        public static string Enhancement = "enhancement";
        public static string Trivial = "trivial";
        public static string Minor = "minor";
        public static string Normal = "normal";
        public static string Major = "major";
        public static string Critical = "critical";
    }

    internal class AvigeroConfig
    {
        public static bool userExperienceVector = true;
        public static bool severityVector = true;

        public static int FirstBug = 14;
        public static int LastBug = 2;
        public static int BugThreshold = 100;

    }
}
