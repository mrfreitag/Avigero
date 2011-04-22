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

        /// <summary>
        /// Parameter for machine learning
        /// </summary>
        private string m_C;
        private string m_G;

        private string m_PredictionResult;
        public string PredictionResult
        {
            get { return m_PredictionResult; }
        }


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
            
            // we only use the short description for now, use long description later
            bool shortDescriptionOnly = true;
            // do not spellcheck at the moment
            bool spellcheck = false;
            SplitAndStem(shortDescriptionOnly, spellcheck);

            CreateWordVectorDictionary();

            CreateTrainingData(AvigeroConfig.userExperienceVector, AvigeroConfig.severityVector);

            bool subSetCreated = CreateTrainingSubSet();
            if (subSetCreated)
            {
                GridSearch();
                Train();
                Predict();
            }

        }

        private void Predict()
        {

            System.Diagnostics.Process stemProcess = new System.Diagnostics.Process();
            string cwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(cwd + @"..\..\Toolchain\libsvm\windows\");
            stemProcess.StartInfo.FileName = "svm-predict.exe";
            stemProcess.StartInfo.CreateNoWindow = false;
            stemProcess.StartInfo.RedirectStandardError = false;
            stemProcess.StartInfo.RedirectStandardOutput = true;
            stemProcess.StartInfo.UseShellExecute = false;

            // Define parameters for subset.py
            string inputVector = GetOut20Vector();
            string model = GetModel();
            string trainParamFile = GetTrainParamFile();

            // First the 100% split
            stemProcess.StartInfo.Arguments = inputVector + " "
                                            + model + " "
                                            + inputVector + ".predict";
            stemProcess.Start();
            m_PredictionResult = stemProcess.StandardOutput.ReadToEnd();
            stemProcess.WaitForExit();

            // Remove strings
            m_PredictionResult = m_PredictionResult.Replace("Accuracy = ", string.Empty);
            m_PredictionResult = m_PredictionResult.Replace(" (classification)", string.Empty);

            // Write info on console
            Console.WriteLine("{0} accuracy for {1}", 3, m_PredictionResult, Project);

            Directory.SetCurrentDirectory(cwd);
        }

        private void Train()
        {
            System.Diagnostics.Process stemProcess = new System.Diagnostics.Process();

            string cwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(cwd + @"..\..\Toolchain\libsvm\windows\");

            stemProcess.StartInfo.FileName = "svm-train.exe";
            stemProcess.StartInfo.CreateNoWindow = false;
            stemProcess.StartInfo.RedirectStandardError = false;
            stemProcess.StartInfo.UseShellExecute = false;

            // Define parameters for subset.py
            string out80Vector = GetOut80Vector();

            // First the 100% split
            stemProcess.StartInfo.Arguments = "-t 2 -c " + m_C + " "
                                            + "-g " + m_G + " "
                                            + out80Vector;
            stemProcess.Start();
            stemProcess.WaitForExit();

            // Move model to output directory
            string source = Project
              + "_en_ttc80.vec.model";

            string dest = "../../" + OutputDirectories.MachineLearningOutput
              + "/"
              + source;

            if (File.Exists(dest))
                File.Delete(dest);

            File.Move(source, dest);
            Directory.SetCurrentDirectory(cwd);
        }

        /// <summary>
        /// Perform the grid search. The last line of the output file
        /// contains the parameters that will be used for training
        /// </summary>
        private void GridSearch()
        {
            System.Diagnostics.Process stemProcess = new System.Diagnostics.Process();
            string cwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(cwd + @"..\..\Toolchain\libsvm\tools\");
            string dir = Directory.GetCurrentDirectory();
            stemProcess.StartInfo.FileName = "python.exe";
            string script = @"grid.py";
            stemProcess.StartInfo.CreateNoWindow = false;
            stemProcess.StartInfo.RedirectStandardError = false;
            stemProcess.StartInfo.RedirectStandardOutput = true;
            stemProcess.StartInfo.UseShellExecute = false;

            // Define parameters for subset.py
            string inputVector = GetOut80Vector();
            string trainParamFile = GetTrainParamFile();

            // First the 100% split
            stemProcess.StartInfo.Arguments = script + " "
                                            + inputVector;
            stemProcess.Start();
            string result = stemProcess.StandardOutput.ReadToEnd();
            stemProcess.WaitForExit();

            string[] tmp = result.Split('\n');
            // last line is empty
            string[] parameters = tmp[tmp.Length - 2].Split(' ');

            // Get the parameters
            m_C = parameters[0];
            m_G = parameters[1];
            Directory.SetCurrentDirectory(cwd);
        }

        /// <summary>
        /// Create a subset of the training data
        /// Up to 500 Bugs there will be an 80%/20% split, after that there will be an absolut
        /// subset of 400 training Bugs and 100 prediction Bugs
        /// </summary>
        private bool CreateTrainingSubSet()
        {
            // Working Directory
            string cwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(cwd + @"..\..\Toolchain\libsvm\tools\");

            // it does not make sense if we have less than 10 RQs
            if (NumClosedBugs < AvigeroConfig.BugThreshold)
            {
                Directory.SetCurrentDirectory(cwd);
                return false;
            }

            int numTrainingRQs = 0;
            int numTestRQs = 0;

            if (NumClosedBugs > 500)
            {
                numTrainingRQs = 400;
                numTestRQs = 100;
            }
            else
            {
                numTrainingRQs = System.Convert.ToInt16(0.8 * NumClosedBugs);
                numTestRQs = System.Convert.ToInt16(0.2 * NumClosedBugs);
            }

            string inputVector;     // the input file
            string out100Vector;    // output file containing all data
            string out80Vector;     // output file containing 80% of the data for training
            string out20Vector;     // output file containing 20% of the data for testing

            System.Diagnostics.Process stemProcess = new System.Diagnostics.Process();
            stemProcess.StartInfo.FileName = "python.exe";
            string script = @"subset.py";
            stemProcess.StartInfo.CreateNoWindow = false;
            stemProcess.StartInfo.RedirectStandardError = false;
            stemProcess.StartInfo.UseShellExecute = false;

            // Define parameters for subset.py
            inputVector = GetInputVector();
            out100Vector = GetOut100Vector();
            out80Vector = GetOut80Vector();
            out20Vector = GetOut20Vector();

            // First the 100% split
            int numSplitRQs = numTrainingRQs + numTestRQs;
            stemProcess.StartInfo.Arguments = script + " "
                                            + inputVector + " "
                                            + numSplitRQs + " "
                                            + out100Vector;
            stemProcess.Start();
            stemProcess.WaitForExit();

            // Then the 80/20 split
            stemProcess.StartInfo.Arguments = script + " "
                                            + out100Vector + " "
                                            + numTrainingRQs + " "
                                            + out80Vector + " "
                                            + out20Vector;
            stemProcess.Start();
            stemProcess.WaitForExit();

            // subset.py ../../Output/MachineLearningOutput/%%G_en_ttc.vec 500 ../../Output/MachineLearningOutput/%%G_en_ttc500.vec
            // subset.py ../../Output/MachineLearningOutput/%%G_en_ttc500.vec 400 ../../Output/MachineLearningOutput/%%G_en_ttc400.vec ../../Output/MachineLearningOutput/%%G_en_ttc100.vec

            Directory.SetCurrentDirectory(cwd);

            return true;
        }

        private string GetTrainParamFile()
        {
            return "../../" + OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc.param";
        }

        private string GetOut20Vector()
        {
            return "../../" + OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc20.vec";
        }

        private string GetOut80Vector()
        {
            return "../../" + OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc80.vec";
        }

        private string GetOut100Vector()
        {
            return "../../" + OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc100.vec";
        }

        private string GetInputVector()
        {
            return "../../" + OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc.vec";
        }

        private string GetModel()
        {
            return "../../" + OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc80.vec.model";
        }

        private void CreateTrainingData(bool userExperienceVector, bool severityVector)
        {
            // 1. Read dict file and create dict vector
            // 2. Read the data file and create the vector

            // 1. Read dict file and create dict vector
            StreamReader en_dict_file = new StreamReader(OutputDirectories.MachineLearningOutput
                    + "/"
                    + Project
                    + "_en.dict");
            ArrayList DictVector = new ArrayList();
            string entry;
            while (((entry = en_dict_file.ReadLine()) != null))
            {
                if (!entry.Equals(string.Empty))
                    DictVector.Add(entry);
            }
            en_dict_file.Close();

            // 2. Read the data file and create the word vector
            // Severity
            StreamReader en_word_file = new StreamReader(OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_sev.stem");

            TextWriter en_dictWriter = new StreamWriter(OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_sev.vec");

            string line = string.Empty;
            string output = string.Empty;
            List<string> usedWords = new List<string>();
            List<int> severities = new List<int>();
            // List of key value pairs to store position and occurrence
            List<KeyValuePair<int, int>> positions = new List<KeyValuePair<int, int>>();
            while (((line = en_word_file.ReadLine()) != null) && (!line.Equals(string.Empty)))
            {
                output = string.Empty;
                string severity = line.Substring(0, 1);
                severities.Add(System.Convert.ToInt16(severity));
                output += severity + " ";
                string description = line.Substring(2);

                string cleaned = CleanedStringWithSpace(description);
                foreach (string token in cleaned.Split(' '))
                {
                    if ((null != token) && !usedWords.Contains(token))
                    {
                        usedWords.Add(token);
                        int index = DictVector.IndexOf(token);
                        if (index > -1)
                            positions.Add(new KeyValuePair<int, int>(index, 1));
                    }
                    else
                    {
                        int position = DictVector.IndexOf(token);
                        // stemming removed already some words
                        // only index the "good ones"
                        if (position > -1)
                        {
                            KeyValuePair<int, int> myPair = positions.Where(x => x.Key.Equals(position)).Single();
                            int value = myPair.Value;
                            value++;
                            positions.Remove(myPair);
                            positions.Add(new KeyValuePair<int, int>(position, value));
                        }
                    }
                }
                positions.Sort(delegate(KeyValuePair<int, int> x, KeyValuePair<int, int> y) { return x.Key.CompareTo(y.Key); });

                // create binary version of the severity word vector
                //foreach (string dictWord in DictVector)
                //{
                //    int dictposition = DictVector.IndexOf(dictWord);
                //    bool res = positions.Exists(x => x.Key.Equals(dictposition));
                //    if ( res )
                //        output += dictposition + ":1 ";
                //    else
                //        output += dictposition + ":-1 ";
                //}

                //This is an old way to create the word vector
                //only mention the words that exist
                foreach (KeyValuePair<int, int> pos in positions)
                {
                    // binary vector
                    output += pos.Key + ":1 ";
                    // non-binary vector
                    //output += pos.Key + ":" + pos.Value + " ";
                }

                positions.Clear();
                usedWords.Clear();
                en_dictWriter.WriteLine(output);
            }
            en_dictWriter.Close();


            // time to close
            StreamReader en_ttc_word_file = new StreamReader(OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc.stem");

            TextWriter en_ttc_dictWriter = new StreamWriter(OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc.vec");

            string ttc_line = string.Empty;
            string ttc_output = string.Empty;
            List<string> usedTtc_Words = new List<string>();
            // List of key value pairs to store position and occurrence
            List<KeyValuePair<int, int>> ttcpositions = new List<KeyValuePair<int, int>>();
            int count = 0;
            while (((ttc_line = en_ttc_word_file.ReadLine()) != null) && (!ttc_line.Equals(string.Empty)))
            {
                ttc_output = string.Empty;
                string ttcbin = ttc_line.Substring(0, 1);
                ttc_output += ttcbin + " ";
                string description = ttc_line.Substring(2);

                string cleaned = CleanedStringWithSpace(description);
                foreach (string token in cleaned.Split(' '))
                {
                    if ((null != token) && !usedTtc_Words.Contains(token))
                    {
                        usedTtc_Words.Add(token);
                        int index = DictVector.IndexOf(token);
                        if (index > -1)
                            ttcpositions.Add(new KeyValuePair<int, int>(index, 1));
                    }
                    else
                    {
                        int position = DictVector.IndexOf(token);
                        if (position > -1)
                        {
                            KeyValuePair<int, int> myPair = ttcpositions.Where(x => x.Key.Equals(position)).Single();
                            int value = myPair.Value;
                            value++;
                            ttcpositions.Remove(myPair);
                            ttcpositions.Add(new KeyValuePair<int, int>(position, value));
                        }
                    }
                }
                ttcpositions.Sort(delegate(KeyValuePair<int, int> x, KeyValuePair<int, int> y) { return x.Key.CompareTo(y.Key); });
                // let's see what severity does
                // Add the severity-vector

                // First define the dimensional shift
                int shift = 0;

                if (severityVector)
                {
                    shift += 7;
                    ttc_output += severities[count] + ":1 ";
                }

                // add the user-vector
                if (userExperienceVector)
                {
                    // AI: Refine experience vector
                    // the experience is currently based on the overall experience of the originator within the
                    // given timeframe. This should be refined, at some point it would make sense to use some sort
                    // of relative experience at the creation time.
                    int currentExperience = System.Convert.ToInt16(m_UserExperienceVector[count]) - 1;
                    double relativeExperience = (double)currentExperience / (double)(m_UserExperienceVector.Max() - 1);
                    double scaledExperience = 2 * relativeExperience - 1;
                    ttc_output += shift + ":" + scaledExperience + " ";
                    shift++;
                }

                // create scaled binary version of the severity word vector
                //foreach (string dictWord in DictVector)
                //{
                //    int dictposition = DictVector.IndexOf(dictWord);
                //    bool res = ttcpositions.Exists(x => x.Key.Equals(dictposition));
                //    int outposition = dictposition + shift;
                //    if (res)
                //        ttc_output += outposition + ":1 ";
                //    else
                //        ttc_output += outposition + ":-1 ";
                //}

                // the old version to create the vector
                foreach (KeyValuePair<int, int> pos in ttcpositions)
                {
                    int shiftpos = pos.Key + shift; // +m_UserExperienceVector.Count;
                    // binary
                    ttc_output += shiftpos + ":1 ";
                    //non-binary
                    //ttc_output += shiftpos + ":" + pos.Value + " ";
                }

                ttcpositions.Clear();
                usedTtc_Words.Clear();
                en_ttc_dictWriter.WriteLine(ttc_output);
                count++;

            }
            en_ttc_dictWriter.Close();

            return;
        }

        /// <summary>
        /// Read all bugs from a specific language and create a dictionary from that
        /// </summary>
        private void CreateWordVectorDictionary()
        {
            // English
            List<string> en_WordDict = new List<string>();
            //            Dictionary<string, int> WordDict = new Dictionary<string, int>();
            StreamReader en_file = new StreamReader(OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_sev.stem");

            TextWriter en_dictWriter = new StreamWriter(OutputDirectories.MachineLearningOutput
                    + "/"
                    + Project
                    + "_en.dict");

            string description;
            while ((description = en_file.ReadLine()) != null)
            {
                // Remove the number at the beginning
                if (!description.Equals(string.Empty))
                {
                    description = description.Substring(2);
                    string cleaned = CleanedStringWithSpace(description);
                    foreach (string token in cleaned.Split(' '))
                    {
                        // Btw, this is where the stop word removal happens
                        if ((null != token) && !en_WordDict.Contains(token) && !IsStopWord(token.ToLower()))
                        {
                            en_WordDict.Add(token);
                            en_dictWriter.WriteLine(token);
                        }
                    }
                }
            }
            en_dictWriter.Close();
        }

        private bool IsStopWord(string token)
        {
            // stop word removal
            // ToDo: go through the list of stop words and try to find a better list
            string en_stop = "a,able,about,across,after,all,almost,also,am,among,an,and,any,are,as,at,be,because,been,but,by,can,cannot,could,dear,did,do,does,doesn't,doesnt,either,else,ever,every,for,from,get,got,had,has,have,he,her,hers,him,his,how,however,i,if,in,into,is,it,its,just,least,let,like,likely,may,me,might,most,must,my,neither,no,nor,not,of,off,often,on,only,or,other,our,own,rather,said,say,says,she,should,since,so,some,than,that,the,their,them,then,there,these,they,this,tis,to,too,twas,us,wants,was,we,were,what,when,where,which,while,who,whom,why,will,with,would,yet,you,your";
            string[] en_stop_array = en_stop.Split(',');
            foreach (string stopword in en_stop_array)
            {
                if (token.Equals(stopword))
                    return true;
            }
            return false;
        }

        private void SplitAndStem(bool shortDescriptionOnly, bool spellcheck)
        {
            IEnumerable<bug> closedBugs = from myBug in RawData
                                          where (myBug.bug_status.Text[0].Equals(BugStatus.Status_Resolved))
                                          select myBug;

            TextWriter englishSeverityWordWriter = new StreamWriter(OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_sev.word");

            TextWriter englishTtcWordWriter = new StreamWriter(OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc.word");

            int numEnglishRQs = 0;

            foreach (bug myBug in closedBugs)
            {
                TimeSpan closingTime = System.Convert.ToDateTime(myBug.delta_ts.Text[0]).Subtract(System.Convert.ToDateTime(myBug.creation_ts.Text[0]));
                int daysToFixBin = GetDaysToFixBin(closingTime.TotalDays);
                // string to determine language from: summary + description
                string description = string.Empty;
                if (shortDescriptionOnly)
                    description = myBug.short_desc.Text[0].ToLower();
                else
                    description = myBug.short_desc.Text[0].ToLower() + " " + myBug.long_desc[0].thetext.Text[0].ToLower();
                description = CleanedStringWithSpace(description);
                // ToDo: Spellcheck
                //if (spellcheck)
                //    description = SpellCheck(description);
                //
                // ToDo: split languages
                string lang = "en";

                // The string to write has the following formatting:
                // binarized severity (6 positions)
                // binarized days to close (4 positions)
                // text
                // ignore non-engish RQs
                if (lang.Equals("en"))
                {
                    // ToDo: Check consistency of this implementation
                    // The description needs to be stemmed separately.
                    // description = StemmedString(description);

                    englishSeverityWordWriter.WriteLine(GetSeverity(myBug) + " " + description);
                    englishTtcWordWriter.WriteLine(daysToFixBin + " " + description);
                    numEnglishRQs++;
                }
                else
                {
                    //numNonEnglishRQs++;
                }

            }

            englishSeverityWordWriter.Close();
            englishTtcWordWriter.Close();

            // Stem - Currently: Only english
            string sevFileToStem = OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_sev.word";

            string sevStemFile = OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_sev.stem";

            System.Diagnostics.Process stemProcess = new System.Diagnostics.Process();
            stemProcess.StartInfo.FileName = "stem.exe";
            stemProcess.StartInfo.Arguments = sevFileToStem + " " + sevStemFile;
            stemProcess.StartInfo.CreateNoWindow = false;
            stemProcess.StartInfo.RedirectStandardError = false;
            stemProcess.StartInfo.UseShellExecute = false;
            stemProcess.Start();
            stemProcess.WaitForExit();


            string ttcFileToStem = OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc.word";

            string ttcStemFile = OutputDirectories.MachineLearningOutput
              + "/"
              + Project
              + "_en_ttc.stem";

            stemProcess.StartInfo.Arguments = ttcFileToStem + " " + ttcStemFile;
            stemProcess.Start();
            stemProcess.WaitForExit();
        }

        private string StemmedString(string description)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cleaned string with space and stop word removal
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string CleanedStringWithSpace(string value)
        {
            string cleaned = value;
            cleaned = cleaned.Replace("&", "");
            cleaned = cleaned.Replace(@"\", "");
            cleaned = cleaned.Replace("/", "");
            cleaned = cleaned.Replace("?", "");
            cleaned = cleaned.Replace("ü", "ue");
            cleaned = cleaned.Replace("Ü", "Ue");
            cleaned = cleaned.Replace("ä", "ae");
            cleaned = cleaned.Replace("Ä", "Ae");
            cleaned = cleaned.Replace("ö", "oe");
            cleaned = cleaned.Replace("Ö", "Oe");
            cleaned = cleaned.Replace("\n", "");
            cleaned = cleaned.Replace("\t", "");
            cleaned = cleaned.Replace("`", "");
            return cleaned;

        }

        /// <summary>
        /// Return the bin in which the days to close belong
        /// 0 : 0-25%
        /// 1 : 25-50%
        /// 2 : 50-75%
        /// 3 : 75-100%%
        /// </summary>
        /// <param name="daysToFix"></param>
        /// <returns></returns>
        private int GetDaysToFixBin(double daysToFix)
        {
            if (daysToFix <= m_LowQuartileDaysToClose)
                return 0;
            else if ((m_LowQuartileDaysToClose < daysToFix) && (daysToFix <= m_MedianDaysToClose))
                return 1;
            else if ((m_MedianDaysToClose < daysToFix) && (daysToFix <= m_HighQuartileDaysToClose))
                return 2;
            else
                return 3;
        }

        /// <summary>
        /// The experience of a user scales with the number of involvements
        /// ... hopefully ...
        /// </summary>
        private void CreateUserExperienceVector(List<bug> RawData)
        {
            // only do it for closed rqs
            IEnumerable<bug> closedBugs = from myBug in RawData
                                       where (myBug.bug_status.Text[0].Equals(BugStatus.Status_Resolved))
                                       select myBug;

            // ToDo: For now we only use the reporters, there might be developers
            // who do not report any bugs, but work on finishing them
            // this needs to be taken into account at some point
            IEnumerable<string> users = from myBug in closedBugs
                                        select myBug. reporter.Text[0];

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
