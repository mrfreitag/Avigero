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
}
