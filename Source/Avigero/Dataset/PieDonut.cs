using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Avigero.Helper;
using System.Globalization;

namespace Avigero.Dataset
{
    /// <summary>
    /// Pie Donut chart for visualization of component development.
    /// </summary>
    class PieDonut
    {
        private string m_Project;
        public string Project
        {
            set { m_Project = value; }
        }

        private List<KeyValuePair<string, int>> m_ProjectWeights;
        public List<KeyValuePair<string, int>> ProjectWeights
        {
            set { m_ProjectWeights = value; }
        }

        public void Create()
        {
            // Read the template file
            TextReader FileReader = null;
            FileReader = File.OpenText("../Templates/" + TemplateFiles.PieDonut);
            string Content = FileReader.ReadToEnd();
            FileReader.Close();

            string pieDataEntries = string.Empty;
            // m_ProjectWeights.Sort();
            // Add entries to the list
            int red = 12;
            int green = 12;
            int blue = 0;
            foreach (KeyValuePair<string, int> projectEntry in m_ProjectWeights)
            {
                red = (red + 12) % 255;
                green = (green + 12) % 255;
                blue = 0; // (blue + 47) % 255;

                pieDataEntries += "{ name: '" 
                    + projectEntry.Key 
                    + "', y: " 
                    + projectEntry.Value 
                    + ", color: 'rgb(" 
                    + red.ToString() 
                    + ","
                    + green 
                    +","
                    + blue 
                    + ")' },";
            }
            // Remove last ,
            pieDataEntries.TrimEnd(',');

            Content = Content.Replace(TemplateTags.tags[(int)TemplateTagNames.PieDataEntries], pieDataEntries);

            // Write the output file
            // Write Output File
            TextWriter timeSeriesText = new StreamWriter("../Output/PieDonuts/" + m_Project + ".htm");
            timeSeriesText.Write(Content);
            timeSeriesText.Close();
        }
    }
}
