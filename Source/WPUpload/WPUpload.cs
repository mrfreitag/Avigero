using System;
using CookComputing.XmlRpc;
using System.Net;
using System.IO;

namespace MetaWeblogApi
{


    /// <summary> 
    /// This struct represents information about a user's blog. 
    /// </summary> 
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    struct UserBlog
    {
        public string url;
        public string blogid;
        public string blogName;
    }


    /// <summary> 
    /// This struct represents information about a user. 
    /// </summary> 
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    struct UserInfo
    {
        public string url;
        public string blogid;
        public string blogName;
        public string firstname;
        public string lastname;
        public string email;
        public string nickname;
    }


    /// <summary> 
    /// This struct represents the information about a category that could be returned by the 
    /// getCategories() method. 
    /// </summary> 
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    struct Category
    {
        public string description;
        public string title;
    }

    /// <summary> 
    /// This struct represents the information about a post that could be returned by the 
    /// editPage(), getRecentPages() and getPage() methods. 
    /// </summary> 
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    struct Page
    {
        public DateTime dateCreated;
        public string description;
        public string title;
        public string postid;
        public string[] categories;
    }

    /// <summary> 
    /// This struct represents the information about a post that could be returned by the 
    /// editPost(), getRecentPosts() and getPost() methods. 
    /// </summary> 
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    struct Post
    {
        public DateTime dateCreated;
        public string description;
        public string title;
        public string postid;
        public string[] categories;
    }

    /// <summary> 
    /// This class can be used to programmatically interact with a Weblog on 
    /// using the MetaWeblog API. 
    /// </summary> 
    [XmlRpcUrl("http://www.avigero.com/xmlrpc.php")]
    class WPMetaWeblog : XmlRpcClientProtocol
    {
        /// <summary> 
        /// Returns the most recent draft and non-draft blog posts sorted in descending order by publish date. 
        /// </summary> 
        /// <param name="blogid"> This should be the string MyBlog, which indicates that the post is being created in the user’s blog. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <param name="password"> The user’s secret word. </param> 
        /// <param name="numberOfPosts"> The number of posts to return. The maximum value is 20. </param> 
        /// <returns></returns> 
        [XmlRpcMethod("metaWeblog.getRecentPosts")]
        public Post[] getRecentPosts(
        string blogid,
        string username,
        string password,
        int numberOfPosts)
        {
            return (Post[])this.Invoke("getRecentPosts", new object[] { blogid, username, password, numberOfPosts });
        }

        /// <summary>
        /// Post a new page to the blog. 
        /// </summary>
        /// <param name="blogid"> This should be the string MyBlog, which indicates that the post is being created in the user’s blog. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <param name="password"> The user’s secret word. </param> 
        /// <param name="post"> A struct representing the content to update. </param> 
        /// <param name="publish"> If false, this is a draft post. </param> 
        /// <returns> The postid of the newly-created post. </returns> 
        [XmlRpcMethod("wp.newPage")]
        public string newPage(
        string blogid,
        string username,
        string password,
        Page content,
        bool publish)
        {
            Uri bypass = new Uri("http://www.avigero.com");
            IWebProxy myProxy = new WebProxy();
            this.Proxy = myProxy;
            bool dobp = this.Proxy.IsBypassed(bypass);

            return (string)this.Invoke("newPage", new object[] { blogid, username, password, content, publish });
        }

        /// <summary> 
        /// Posts a new entry to a blog. 
        /// </summary> 
        /// <param name="blogid"> This should be the string MyBlog, which indicates that the post is being created in the user’s blog. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <param name="password"> The user’s secret word. </param> 
        /// <param name="post"> A struct representing the content to update. </param> 
        /// <param name="publish"> If false, this is a draft post. </param> 
        /// <returns> The postid of the newly-created post. </returns> 
        [XmlRpcMethod("metaWeblog.newPost")]
        public string newPost(
        string blogid,
        string username,
        string password,
        Post content,
        bool publish)
        {
            Uri bypass = new Uri("http://www.avigero.com");
            IWebProxy myProxy = new WebProxy();
            this.Proxy = myProxy;
            bool dobp = this.Proxy.IsBypassed(bypass);

            return (string)this.Invoke("newPost", new object[] { blogid, username, password, content, publish });
        }

        /// <summary> 
        /// Edits an existing entry on a blog. 
        /// </summary> 
        /// <param name="postid"> The ID of the post to update. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <param name="password"> The user’s secret word. </param> 
        /// <param name="post"> A struct representing the content to update. </param> 
        /// <param name="publish"> If false, this is a draft post. </param> 
        /// <returns> Always returns true. </returns> 
        [XmlRpcMethod("metaWeblog.editPost")]
        public bool editPost(
        string postid,
        string username,
        string password,
        Post content,
        bool publish)
        {

            return (bool)this.Invoke("editPost", new object[] { postid, username, password, content, publish });
        }

        /// <summary> 
        /// Deletes a post from the blog. 
        /// </summary> 
        /// <param name="appKey"> This value is ignored. </param> 
        /// <param name="postid"> The ID of the post to update. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <param name="password"> The user’s secret word. </param> 
        /// <param name="post"> A struct representing the content to update. </param> 
        /// <param name="publish"> This value is ignored. </param> 
        /// <returns> Always returns true. </returns> 
        [XmlRpcMethod("blogger.deletePost")]
        public bool deletePost(
        string appKey,
        string postid,
        string username,
        string password,
        bool publish)
        {

            return (bool)this.Invoke("deletePost", new object[] { appKey, postid, username, password, publish });
        }


        /// <summary> 
        /// Returns information about the user’s space. An empty array is returned if the user does not have a space. 
        /// </summary> 
        /// <param name="appKey"> This value is ignored. </param> 
        /// <param name="postid"> The ID of the post to update. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <returns> An array of structs that represents each of the user’s blogs. The array will contain a maximum of one struct, since a user can only have a single space with a single blog. </returns> 
        [XmlRpcMethod("blogger.getUsersBlogs")]
        public UserBlog[] getUsersBlogs(
        string appKey,
        string username,
        string password)
        {

            return (UserBlog[])this.Invoke("getUsersBlogs", new object[] { appKey, username, password });
        }

        /// <summary> 
        /// Returns basic user info (name, e-mail, userid, and so on). 
        /// </summary> 
        /// <param name="appKey"> This value is ignored. </param> 
        /// <param name="postid"> The ID of the post to update. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <returns> A struct containing profile information about the user. 
        /// Each struct will contain the following fields: nickname, userid, url, e-mail, 
        /// lastname, and firstname. </returns> 
        [XmlRpcMethod("blogger.getUserInfo")]
        public UserInfo getUserInfo(
        string appKey,
        string username,
        string password)
        {

            return (UserInfo)this.Invoke("getUserInfo", new object[] { appKey, username, password });
        }


        /// <summary> 
        /// Returns a specific entry from a blog. 
        /// </summary> 
        /// <param name="postid"> The ID of the post to update. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <param name="password"> The user’s secret word. </param> 
        /// <returns> Always returns true. </returns> 
        [XmlRpcMethod("metaWeblog.getPost")]
        public Post getPost(
        string postid,
        string username,
        string password)
        {

            return (Post)this.Invoke("getPost", new object[] { postid, username, password });
        }

        /// <summary> 
        /// Returns the list of categories that have been used in the blog. 
        /// </summary> 
        /// <param name="blogid"> This should be the string MyBlog, which indicates that the post is being created in the user’s blog. </param> 
        /// <param name="username"> The name of the user’s space. </param> 
        /// <param name="password"> The user’s secret word. </param> 
        /// <returns> An array of structs that contains one struct for each category. Each category struct will contain a description field that contains the name of the category. </returns> 
        [XmlRpcMethod("metaWeblog.getCategories")]
        public Category[] getCategories(
        string blogid,
        string username,
        string password)
        {

            return (Category[])this.Invoke("getCategories", new object[] { blogid, username, password });
        }

        /// <summary> 
        /// The main entry point for the application. 
        /// </summary> 
        [STAThread]
        static void Main(string[] args)
        {

            WPMetaWeblog mw = new WPMetaWeblog();
            Console.Write("Please enter your username: ");
            string username = Console.ReadLine();
            Console.Write("Please enter your password: ");
            string password = Console.ReadLine();
            mw.Credentials = new NetworkCredential(username, password);

            string inputFolder = @"..\Output\TimeSeries\";

            try
            {

                // Space3Pqc3yUo00Y0 
                foreach (string file in Directory.GetFiles(inputFolder))
                {
                    FileInfo finfo = new FileInfo(file);

                    Page page = new Page();
                    page.categories = new string[] { "Time Series" };
                    page.title = finfo.Name.Replace(".htm", string.Empty);

                    TextReader FileReader = null;
                    FileReader = File.OpenText(file);
                    string Content = FileReader.ReadToEnd();
                    FileReader.Close();

                    page.description = Content;
                    page.dateCreated = DateTime.Now;

                    string id = mw.newPage("Avigero", username, password, page, true);
                }
                //Post post = new Post();
                //post.categories = new string[] { "Test Posts" };
                //post.title = "Tets 3";
                //post.description = "This is my 3rd test post";
                //post.dateCreated = DateTime.Now;


                //post.title = "Test 3 (typo fixed)";

                //mw.editPost(id, username, password, post, true);

                //post = mw.getPost(id, username, password);
                //Console.WriteLine("Just edited the post with the title '{0}' at '{1}'", post.title, post.dateCreated);

                ///* display list of categories used on the blog */
                //Category[] categories = mw.getCategories("MyBlog", username, password);

                //foreach (Category c in categories)
                //{
                //    Console.WriteLine("Category: {0}", c.description);
                //}

                ///* display title of the ten most recent posts */
                //Post[] posts = mw.getRecentPosts("MyBlog", username, password, 10);

                //foreach (Post p in posts)
                //{
                //    Console.WriteLine("Post Title: {0}", p.title);
                //}

                //mw.deletePost(String.Empty, id, username, password, true);
                //Console.WriteLine("The post entitled '{0}' has been deleted", post.title);

                ///* get info on user's blogs */
                //UserBlog[] blogs = mw.getUsersBlogs(String.Empty, username, password);

                //foreach (UserBlog b in blogs)
                //{
                //    Console.WriteLine("The URL of '{0}' is {1}", b.blogName, b.url);
                //}

                ///* get info on the user */
                //UserInfo user = mw.getUserInfo(String.Empty, username, password);
                //Console.WriteLine("{0} {1} ({2}) is the owner of the space whose URL is {3}", user.firstname, user.lastname, user.email, user.url);


            }
            catch (XmlRpcFaultException xrfe)
            {
                Console.WriteLine("ERROR: {0}", xrfe.FaultString);
            }

            Console.ReadLine();
        }

    }

}
