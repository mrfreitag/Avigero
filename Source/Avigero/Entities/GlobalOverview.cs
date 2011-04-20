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
        public void Initialize()
        {
            m_Project = string.Empty;
            m_Products = new List<string>();
            m_Components = new List<string>();
        }

        public string Project
        {
            get { return m_Project; }
            set { m_Project = value; }
        }

        public void AddProduct(string Product)
        {
            if (!m_Products.Contains(Product))
                m_Products.Add(Product);
        }

        public void AddComponent(string Component)
        {
            if (!m_Components.Contains(Component))
                m_Components.Add(Component);
        }

        public void ListProducts()
        {
            Console.WriteLine("Listing all products for {0}", m_Project);
            foreach (string product in m_Products)
                Console.WriteLine(product);
        }

        public void ListComponents()
        {
            Console.WriteLine("Listing all components for {0}", m_Project);
            foreach (string component in m_Components)
                Console.WriteLine(component);
        }
    }
}
