using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Linq;

using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CommandLine;

using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace TwitterListener
{
    class Program
    {
        static bool exit = false;

        static void Main(string[] args)
        {
            // Builder for command line args
            Parser parser = new Parser(config => config.HelpWriter = Console.Error);
            ParserResult<Options> result = parser.ParseArguments<Options>(args);
            if (result.Tag == ParserResultType.NotParsed)
                return;
            Parsed<Options> parsed = (Parsed<Options>) result;

            // Init variables
            string username = parsed.Value.Username;
            string soundPath = parsed.Value.Sound;
            string dataFile = $"{username}.json";
            string spaceLink = null;
            string profileName = parsed.Value.Profile;
            ulong lastTweetID = 0;
            
            // Init sound file
            Alarm alarm = new Alarm();
            if (!alarm.LoadSoundFile(soundPath))
                return;

            // Check for {username}.json and create if it doesn't exist
            try
            {
                FileStream fs = new FileStream(dataFile, FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                string jsonString = sr.ReadToEnd();
                JObject jo = JObject.Parse(jsonString);
                try
                {
                    lastTweetID = jo["LastTweetID"].ToObject<ulong>(); // Just in case somebody thinks it's a funny idea to create an empty json file beforehand
                }
                catch
                {
                    lastTweetID = 0;
                }
                sr.Close(); // I get an IO exception down in UpdateDataFile if I don't close it explicitly
                fs.Close();
            }
            catch (FileNotFoundException)
            {

            }

            // Initialize Selenium webdriver and stuff
            WebdriverHandler.Type driverType = parsed.Value.Firefox ? WebdriverHandler.Type.Firefox : WebdriverHandler.Type.Chrome;
            WebDriver driver = WebdriverHandler.Init(driverType, profileName);
            driver.Navigate().GoToUrl(new Uri($"https://twitter.com/{username}")); // Navigate to said page
            // If it throws exception here might as well not use this program at all, not gonna try-catch this

            // A program termination event so that we can dispose of the webdriver whenever the program is terminated and we won't have zombie processes running around willy nilly
            AppDomain.CurrentDomain.ProcessExit += OnProcessTerminated;
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10); // A delay of ten secs till exception is thrown
            ListenerLog.WriteLine("You can exit the program any time by hitting the Escape button. DO NOT USE termination character, nor should you kill the process suddenly or " +
                "otherwise you risk the webdriver littering your Local/Temp folder with unwanted profile files", ConsoleColor.DarkMagenta);
            Thread keyListener = new Thread(() => ListenforQuitKey());
            keyListener.Start();

            // *** Operation under way ***
            while (!exit)
            {
                IWebElement element;
                while (true) // Keep trying to fetch the last Tweet element till it gets loaded up
                {
                    try
                    {
                        element = driver.FindElement(By.XPath("/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/div[2]/section/div/div/div[1]/div/div/article/div/div/div/div[2]/div[2]/div[1]/div/div/div[1]"));
                        // This XPath corresponds for the last Tweet sent, for now
                        break;
                    }
                    catch (NoSuchElementException) // If for some reason it can't find the element, it will throw this
                    {
                        // What if we just deleted cookies and rerolled?
                        driver.Manage().Cookies.DeleteAllCookies();
                        driver.Navigate().Refresh();
                    }
                }
                element = element.FindElements(By.TagName("a"))[1]; // Somehow it also latches to that first hyperlink up in username
                string tweetUser;
                ulong tweetID;
                string link = element.GetAttribute("href");
                SeparateUsernameandTweetID(link, out tweetUser, out tweetID); // Get the Tweet link
                if (!username.Equals(tweetUser) && tweetID != lastTweetID) // If username isn't equal, it's probably a retweet
                {
                    lastTweetID = tweetID;
                    ListenerLog.WriteLine($"New Retweet from {username} at {DateTime.Now} local time", ConsoleColor.Green);
                    alarm.Play();
                    UpdateDataFile(tweetID, dataFile);
                    if (parsed.Value.Clipboard.Contains("retweet")) CopytoClipboard(link);
                }
                else if (tweetID < lastTweetID) // This is very unlikely to happen but it means they've actually deleted their last Tweet
                {
                    lastTweetID = tweetID;
                    ListenerLog.WriteLine($"Last Tweet from {username} has a lower ID than stored, did they delete something? [{DateTime.Now} local time]", ConsoleColor.DarkGreen);
                    //alarm.Play(); // Don't warn when Tweets deleted?
                    UpdateDataFile(tweetID, dataFile);
                }
                else if (tweetID != lastTweetID) // Otherwise, it's a direct tweet
                {
                    lastTweetID = tweetID;
                    ListenerLog.WriteLine($"New Tweet from {username} at {DateTime.Now} local time", ConsoleColor.Green);
                    alarm.Play();
                    UpdateDataFile(tweetID, dataFile);
                    if (parsed.Value.Clipboard.Contains("tweet")) CopytoClipboard(link);
                }
                // Check if the user is running a Twitter Space and if the user let us use profiles
                if (!string.IsNullOrEmpty(profileName))
                {
                    element = driver.FindElement(By.XPath("/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/div[1]/div/div[1]/div[1]/div[2]/div/div[2]/div[1]"))
                        .FindElement(By.TagName("a"));
                    string temp = element.GetAttribute("href");
                    if (temp.Contains("spaces") && !temp.Equals(spaceLink))
                    {
                        spaceLink = temp;
                        ListenerLog.WriteLine($"{username} has just started up a Tweet Space! [{DateTime.Now} local time]", ConsoleColor.DarkCyan);
                        alarm.Play();
                        if (parsed.Value.Clipboard.Contains("space")) CopytoClipboard(temp);
                    }
                }
                while (true)
                {
                    try
                    {
                        driver.Navigate().Refresh();
                        break;
                    }
                    catch (WebDriverException)
                    {
                        
                    }
                }
            }
            driver.Quit();
            return;
        }

        private static void OnProcessTerminated(object sender, EventArgs e)
        {
            WebdriverHandler.ShutdownDriver();
        }

        private static void ListenforQuitKey()
        {
            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    ListenerLog.WriteLine("Shutting down...", ConsoleColor.DarkYellow);
                    exit = true;
                    return;
                }
            }
        }

        private static void UpdateDataFile(ulong tweetID, string dataFile)
        {
            Dictionary<string, ulong> jsonDict = new Dictionary<string, ulong>
            {
                {"LastTweetID", tweetID }
            };

            string jsonString = JsonConvert.SerializeObject(jsonDict, Formatting.Indented);
            FileStream fs = new FileStream(dataFile, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(jsonString);
            sw.Close(); // Let's also close here just in case
            fs.Close();
        }

        private static void SeparateUsernameandTweetID(string href, out string username, out ulong tweetID) // It's important that we separate username and Tweet ID so that we can deduce if it's a retweet etc.
        {
            const string twitter = "https://twitter.com/";
            const string status = "status/"; // I'm lazy to crank the counting to see how long these two are
            username = href.Substring(href.IndexOf(twitter) + twitter.Length);
            username = username.Substring(0, username.IndexOf("/"));
            tweetID = ulong.Parse(href.Substring(href.IndexOf(status) + status.Length));
        }

        private static void CopytoClipboard(string link)
        {
            Thread thread = new Thread(() => Clipboard.SetText(link));
            thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            thread.Start();
            thread.Join();
        }
    }
}
