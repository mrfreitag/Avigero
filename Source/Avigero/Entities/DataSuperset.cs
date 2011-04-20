using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Avigero.Helper;
using System.IO;

namespace Avigero.Entities
{
    /// <summary>
    /// A data superset is the aggregated information about a package, component, or even a whole
    /// project. Depending on the size of the project this can take quite some time to aggregate
    /// the data, but hey, it works!
    /// </summary>
    class DataSuperset
    {
        public List<bug> RawData;
        public int NumberOfBugs;

        public int NumClosedBugs;

        public DateTime FirstBug;
        public DateTime LastBug;

        public string Project;

        public double m_AverageDaysToClose;

        /// <summary>
        /// The low quartile of days to close
        /// 25% of bugs are fixed within that timeframe
        /// </summary>
        public double m_LowQuartileDaysToClose;

        /// <summary>
        /// The median days to close
        /// 50% of bugs are fixed within that timeframe
        /// </summary>
        public double m_MedianDaysToClose; // Quartile2 ;-)

        /// <summary>
        /// The high quartile of days to close
        /// 75% of bugs are fixed whithin that timeframe
        /// </summary>
        public double m_HighQuartileDaysToClose;

        public double m_StdDevDaysToClose;

        private List<int> m_UserExperienceVector;

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
        private string m_Name;

        private List<KeyValuePair<string, string>> m_ProductComponentTree;

        public void AddProductComponent(string product, string component)
        {
            KeyValuePair<string, string> pc = new KeyValuePair<string, string>(product, component);
            if (!m_ProductComponentTree.Contains(pc))
                m_ProductComponentTree.Add(pc);
        }

        /// <summary>
        /// Initialize the data superset during construction
        /// </summary>
        public DataSuperset()
        {
            RawData = new List<bug>();
            m_ProductComponentTree = new List<KeyValuePair<string, string>>();
            m_UserExperienceVector = new List<int>();
        }
        /// <summary>
        /// Calculate everything out of the bug reports.
        /// This method leads to the heart of the application: machine learning based prediction
        /// For that some preprocessing needs to be done as well:
        /// 1. Language prediction
        /// 2. Part of speech tagging
        /// 3. Stop word removal
        /// 4. Stemming
        /// 5. Term-Document Matrix
        /// 6. Normalization
        /// 7. SVM based training and prediction
        /// </summary>
        public void Evaluate()
        {
            NumClosedBugs = 0;
            // Basic information like, number of bugs, unique list of products and components,
            // date of first and last bug
            NumberOfBugs = RawData.ToArray().Length;

            IEnumerable<string> products = from myBug in RawData select myBug.product.Text[0];
            string[] uniqueProducts = products.Distinct().ToArray();

            IEnumerable<string> components = from myBug in RawData select myBug.component.Text[0];
            string[] uniqueComponents = components.Distinct().ToArray();

            // ToDo: Get list of severities to build histogram
            // Gain data for severities histogram

            FirstBug = GetFirstBugDate(RawData);
            LastBug = GetLastBugDate(RawData);

            // Calculate the averages (days to fix, median, quartiles)
            CalculateAverages();

            // ToDo: List of Unique Users
            // ToDo: Location activity. Can we get the origin of a bug?

            // This is where the machine learning happens
            CreateUserExperienceVector(RawData);
        }

        /// <summary>
        /// The experience of a user scales with the number of involvements
        /// ... hopefully ...
        /// </summary>
        private void CreateUserExperienceVector(List<bug> RawData)
        {
            // only do it for closed rqs
            IEnumerable<bug> closedBugs = from myBug in RawData
                                       where (myBug.Equals(BugStatus.Status_Resolved))
                                       select myBug;

            IEnumerable<string> users = from myBug in closedBugs
                                        select myBug.reporter.Text[0];

            IEnumerable<string> distinctUsers = users.Distinct();
            List<KeyValuePair<string, int>> userExperienceList = new List<KeyValuePair<string, int>>();

            foreach (string user in distinctUsers)
            {
                int experience = users.Count(x => x.Equals(user));
                userExperienceList.Add(new KeyValuePair<string, int>(user, experience));
            }

            // Create the vector as list
            foreach (string user in users)
            {
                // user:experience
                KeyValuePair<string, int> myUser = userExperienceList.Where(x => x.Key.Equals(user)).Single();
                // 7 reserved for severities
                m_UserExperienceVector.Add(myUser.Value);
            }

            // For verification
            TextWriter en_Writer = new StreamWriter(OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en.txt");

            foreach (bug myBug in RawData)
            {
                string closed = "y";
                NumClosedBugs++;
                if (!myBug.bug_status.Text[0].Equals(BugStatus.Status_Resolved))
                {
                    closed = "n";
                    NumClosedBugs--;
                }

                int severity = GetSeverity(myBug);

                en_Writer.WriteLine(severity + " "
                                  + closed + " "
                                  + myBug.reporter.Text[0] + " "
                                  + myBug.short_desc.Text[0]);
            }
            en_Writer.Close();
        }

        private int GetSeverity(bug myBug)
        {
            if (myBug.bug_severity.Text[0].Equals(Severities.enhancement))
                return 6;
            else if (myBug.bug_severity.Text[0].Equals(Severities.trivial))
                return 5;
            else if (myBug.bug_severity.Text[0].Equals(Severities.minor))
                return 4;
            else if (myBug.bug_severity.Text[0].Equals(Severities.normal))
                return 3;
            else if (myBug.bug_severity.Text[0].Equals(Severities.major))
                return 2;
            else if (myBug.bug_severity.Text[0].Equals(Severities.critical))
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// Calculates the average days to close for all closed RQs.
        /// </summary>
        private void CalculateAverages()
        {
            // Days to Fix - Average, Median, StdDev
            IEnumerable<bug> closedBugs = from myBug in RawData
                                          where (myBug.bug_status.Text[0].Equals(BugStatus.Status_Resolved))
                                          select myBug;

            List<double> daysToFix = new List<double>();
            foreach (bug myBug in closedBugs)
            {
                TimeSpan closingTime = System.Convert.ToDateTime(myBug.delta_ts.Text[0]).Subtract(System.Convert.ToDateTime(myBug.creation_ts.Text[0]));
                daysToFix.Add(closingTime.TotalDays);
            }

            daysToFix.Sort();
            if (!daysToFix.Count.Equals(0))
            {
                int MedianPos = (int)(daysToFix.ToArray().Length / 2);
                int LowPos = (int)(daysToFix.ToArray().Length / 4);
                int HigPos = MedianPos + LowPos;

                m_MedianDaysToClose = daysToFix.ToArray()[MedianPos];
                m_HighQuartileDaysToClose = daysToFix.ToArray()[HigPos];
                m_LowQuartileDaysToClose = daysToFix.ToArray()[LowPos];

                m_AverageDaysToClose = daysToFix.Average();
            }
            else
            {
                m_MedianDaysToClose = 0;
                m_LowQuartileDaysToClose = 0;
                m_HighQuartileDaysToClose = 0;
                m_AverageDaysToClose = 0;
            }

            // Standard Deviation
            m_StdDevDaysToClose = GetStandardDeviation(daysToFix);
        }

        /// <summary>
        /// Calculate the standard deviation of the days to fix a bug
        /// </summary>
        /// <param name="doubleList"></param>
        /// <returns></returns>
        private double GetStandardDeviation(List<double> doubleList)
        {
            if (doubleList.Count.Equals(0))
                return 0;

            double average = doubleList.Average();
            double sumOfDerivation = 0;
            foreach (double value in doubleList)
            {
                sumOfDerivation += (value) * (value);
            }
            double sumOfDerivationAverage = sumOfDerivation / doubleList.Count;
            return Math.Sqrt(sumOfDerivationAverage - (average * average));
        }


        public void ListStatistics()
        {
            Console.WriteLine("Statistics for {0}", m_Name);
//            Console.WriteLine("Number of Products: {0}", m_Products.ToArray().Length);
//            Console.WriteLine("Number of Components {0}", m_Components.ToArray().Length);

            ListProductComponentTree();

            Console.WriteLine("Average days to close: {0:N2}", m_AverageDaysToClose);
            Console.WriteLine("Standard deviation: {0:N2}", m_StdDevDaysToClose);
            Console.WriteLine("Low quartile: {0:N2}", m_LowQuartileDaysToClose);
            Console.WriteLine("Median: {0:N2}", m_MedianDaysToClose);
            Console.WriteLine("High quartile: {0:N2}", m_HighQuartileDaysToClose);
        }

        private void ListProductComponentTree()
        {
            // First get the list of products
            IEnumerable<string> products = from myPair in m_ProductComponentTree select myPair.Key;

            foreach (string product in products)
            {
                IEnumerable<string> components = from myPair in m_ProductComponentTree 
                                                 where myPair.Key.Equals(product) 
                                                 select myPair.Value;
                Console.WriteLine("Product: \t {0}", product);
                foreach (string component in components)
                    Console.WriteLine("\t\t{0}", component);
            }
        }

        private DateTime GetLastBugDate(List<bug> RawData)
        {
            IEnumerable<string> creationDate = from myBug in RawData
                                               select myBug.creation_ts.Text[0];
            // Introducing the Y1K bug...
            DateTime lastBugDate = new DateTime(1000, 1, 1);
            foreach (string dateString in creationDate)
            {
                DateTime tmp = System.Convert.ToDateTime(dateString);
                if (tmp > lastBugDate)
                    lastBugDate = tmp;
            }
            return lastBugDate;
        }

        private DateTime GetFirstBugDate(List<bug> RawData)
        {
            IEnumerable<string> creationDate = from myBug in RawData
                                               select myBug.creation_ts.Text[0];
            // Introducing the Y3K bug...
            DateTime firstBugDate = new DateTime(3000, 1, 1);
            foreach (string dateString in creationDate)
            {
                DateTime tmp = System.Convert.ToDateTime(dateString);
                if (tmp < firstBugDate)
                    firstBugDate = tmp;
            }
            return firstBugDate;
        }
    }
}
