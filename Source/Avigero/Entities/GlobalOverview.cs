using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avigero.Entities
{
    enum Severities
    {
        enhancement = 0,
        trivial,
        minor,
        normal,
        major,
        critical
    }
    /// <summary>
    /// Global Overview contains, as the name implies, overview information about all products
    /// and components of the project.
    /// </summary>
    class GlobalOverview
    {
        private static string m_Project;
        private static List<string> m_Products;
        private static List<string> m_Components;

        /// <summary>
        /// Count of bug severities for histogram
        /// </summary>
        private static int[] m_BugSeverities = new int[6];

        /// <summary>
        /// 
        /// </summary>
        public void Initialize
        {
           
        }
    }
}
