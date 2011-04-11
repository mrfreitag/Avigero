using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace SortAndSplit
{
    class SortAndSplit
    {
        static void Main(string[] args)
        {
            string inputFolder = @"..\BugzillaDBs\";
            foreach (string folder in Directory.GetDirectories(inputFolder))
            {
                // Check if all the output folders exist
                CheckOutputFolders(folder);
                // First loop: Create the blocks. Split the folders by blocks of 1000 files
                // Oh... I like to start counting at one...
                foreach (string file in Directory.GetFiles(folder))
                {
                    FileInfo finfo = new FileInfo(file);
                    int filecount = System.Convert.ToInt32(finfo.Name.Remove(finfo.Name.Length-4));
                    int block = (filecount / 1000);
                    CheckForBlockFolder(folder, block);
                    if (finfo.Length == 0)
                    {
                        MoveFileToUnsorted(finfo, folder);
                    }
                    else
                    {

                        TextReader fileReader = new StreamReader(finfo.FullName, Encoding.UTF8);
                        XmlSerializer rqSerializer = new XmlSerializer(typeof(bugzilla));
                        bugzilla BugzillaBug;
                        try
                        {
                            BugzillaBug = (bugzilla)rqSerializer.Deserialize(fileReader);
                            fileReader.Close();
                            bug Bug = BugzillaBug.bug[0];
                            if (null != Bug.product)
                            {
                                string product = Bug.product.Text[0];
                                string component = Bug.component.Text[0];
                                // Console.WriteLine("{0} - {1}", product, component);

                                MakeValidNames(ref product, ref component);

                                CopyFileToPackages(finfo, folder, product, component);
                                CopyFileToBlock(finfo, folder, block);
                                MoveToSorted(finfo, folder);
                            }
                            else
                            {
                                MoveToUnsorted(finfo, folder);
                            }
                        }
                        catch
                        {
                            fileReader.Close();
                            MoveToInvalid(finfo, folder);
                            continue;
                        }
                    }
                }
            }
        }

        private static void MoveToInvalid(FileInfo finfo, string folder)
        {
            string source = folder + @"\" + finfo.Name;
            string dest = folder + @"\Invalid\" + finfo.Name;
            Console.WriteLine("Move {0} to {1}", source, dest);
            File.Move(source, dest);
        }

        private static void MakeValidNames(ref string product, ref string component)
        {
            // check for product folder
            foreach (char lDisallowed in Path.GetInvalidPathChars())
            {
                string toreplace = lDisallowed.ToString();
                product = product.Replace(toreplace, "");
                component = component.Replace(toreplace, "");
            }

            foreach (char lDisallowed in Path.GetInvalidFileNameChars())
            {
                string toreplace = lDisallowed.ToString();
                product = product.Replace(toreplace, "");
                component = component.Replace(toreplace, "");
            }
        }

        private static void MoveToUnsorted(FileInfo finfo, string folder)
        {
            string source = folder + @"\" + finfo.Name;
            string dest = folder + @"\Unsorted\" + finfo.Name;
            Console.WriteLine("Move {0} to {1}", source, dest);
            File.Move(source, dest);
        }

        private static void MoveToSorted(FileInfo finfo, string folder)
        {
            string source = folder + @"\" + finfo.Name;
            string dest = folder + @"\Sorted\" + finfo.Name;
            Console.WriteLine("Move {0} to {1}", source, dest);
            File.Move(source, dest);
        }

        private static void CopyFileToPackages(FileInfo finfo, string folder, string product, string component)
        {
            CheckForComponentFolder(folder, product, component);
            string source = folder + @"\" + finfo.Name;
            string dest = folder + @"\Packages\" + product + @"\" + component + @"\" + finfo.Name;
            Console.WriteLine("Copy {0} to {1}", source, dest);
            File.Copy(source, dest, true);
        }

        private static void CheckForComponentFolder(string folder, string product, string component)
        {
            if (!Directory.Exists(folder + @"\Packages\" + product))
            {
                Directory.CreateDirectory(folder + @"\Packages\" + product);
                Console.WriteLine("Product {0}", product);
            }
            // check for component folder
            if (!Directory.Exists(folder + @"\Packages\" + product + @"\" + component))
            {
                Directory.CreateDirectory(folder + @"\Packages\" + product + @"\" + component);
                Console.WriteLine("Product {0}, Component {1}", product, component);
            }
        }

        private static void MoveFileToUnsorted(FileInfo finfo, string folder)
        {
            string source = folder + @"\" + finfo.Name;
            string dest = folder + @"\Unsorted\" + finfo.Name;
            Console.WriteLine("Move {0} to {1}", source, dest);
            File.Move(source, dest);
        }

        private static void CopyFileToBlock(FileInfo finfo, string folder, int blockcount)
        {            
            string source = folder + @"\" + finfo.Name;
            string dest = folder + @"\Blocks\Block" + blockcount + @"\" + finfo.Name;
            Console.WriteLine("Copy {0} to {1}", source, dest);
            File.Copy(source, dest, true);
        }

        private static void CheckForBlockFolder(string folder, int block)
        {
            if (!Directory.Exists(folder + @"\Blocks\Block" + block))
            {
                Directory.CreateDirectory(folder + @"\Blocks\Block" + block);
                Console.WriteLine("Block {0} created in {1}", block, folder);
            }
        }

        private static void CheckOutputFolders(string folder)
        {
            if (!Directory.Exists(folder + @"\Sorted"))
            {
                Directory.CreateDirectory(folder + @"\Sorted");
                Console.WriteLine("Sorted created in {0}", folder);
            }
            if (!Directory.Exists(folder + @"\Unsorted"))
            {
                Directory.CreateDirectory(folder + @"\Unsorted");
                Console.WriteLine("Unsorted created in {0}", folder);
            }
            if (!Directory.Exists(folder + @"\Packages"))
            {
                Directory.CreateDirectory(folder + @"\Packages");
                Console.WriteLine("Packages created in {0}", folder);
            }
            if (!Directory.Exists(folder + @"\Blocks"))
            {
                Directory.CreateDirectory(folder + @"\Blocks");
                Console.WriteLine("Blocks created in {0}", folder);
            }
            if (!Directory.Exists(folder + @"\Invalid"))
            {
                Directory.CreateDirectory(folder + @"\Invalid");
                Console.WriteLine("Invalid created in {0}", folder);
            }
        }
    }
}
