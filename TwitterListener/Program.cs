using System;
using System.Media;
using System.IO;
using System.Collections.Generic;

using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CommandLine;

using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using System.Diagnostics;

namespace TwitterListener
{
    class Program
    {
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
            WebDriver driver = WebdriverHandler.Init(driverType);
            driver.Navigate().GoToUrl(new Uri($"https://twitter.com/{username}")); // Navigate to said page
            // If it throws exception here might as well not use this program at all, not gonna try-catch this

            // A program termination event so that we can dispose of the webdriver whenever the program is terminated and we won't have zombie processes running around willy nilly
            AppDomain.CurrentDomain.ProcessExit += OnProcessTerminated;

            // *** Operation under way ***
            while (true)
            {
                IWebElement element;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (true) // Keep trying to fetch the last Tweet element till it gets loaded up
                {
                    try
                    {
                        element = driver.FindElement(By.XPath("/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/div[2]/section/div/div/div[1]/div/div/article/div/div/div/div[2]/div[2]/div[1]/div/div/div[1]"));
                        // This XPath corresponds for the last Tweet sent, for now
                        break;
                    }
                    catch (NoSuchElementException) // If the page hasn't loaded up yet, it will throw this
                    {
                        if (sw.ElapsedMilliseconds > 10000) // We have been botted, let's restart the webdriver
                        {
                            sw.Reset();
                            ListenerLog.WriteLine($"Failed to get Tweet data, Twitter probably got up to our jig, restarting the webdriver... [{DateTime.Now} local time]", ConsoleColor.Red);
                            driver.Manage().Cookies.DeleteAllCookies();
                            WebdriverHandler.RestartDriver(ref driver, driverType, username);
                        }
                    }
                }
                element = element.FindElements(By.TagName("a"))[1]; // Somehow it also latches to that first hyperlink up in username
                string tweetUser;
                ulong tweetID;
                SeparateUsernameandTweetID(element.GetAttribute("href"), out tweetUser, out tweetID); // Get the Tweet link
                if (!username.Equals(tweetUser) && tweetID != lastTweetID) // If username isn't equal, it's probably a retweet
                {
                    lastTweetID = tweetID;
                    ListenerLog.WriteLine($"New Retweet from {username} at {DateTime.Now} local time", ConsoleColor.Green);
                    alarm.Play();
                    UpdateDataFile(tweetID, dataFile);
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
                }
                // Will need to implement profile integration if this needs to be a thing
                // Check if the user is running a Twitter Space
                /*
                element = driver.FindElement(By.XPath("/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/div[1]/div/div[1]/div[1]/div[2]/div/div[2]/div[1]"))
                    .FindElement(By.TagName("a"));
                string temp = element.GetAttribute("href");
                if (temp.Contains("spaces") && !temp.Equals(spaceLink))
                {
                    spaceLink = temp;
                    ListenerLog.WriteLine($"{username} has just started up a Tweet Space! [{DateTime.Now} local time]", ConsoleColor.DarkCyan);
                    alarm.Play();
                }
                */
                while (true)
                {
                    try
                    {
                        driver.Navigate().Refresh();
                        break;
                    }
                    catch (WebDriverException) // Sometimes refreshing might fail and Selenium throws an exception if it doesn't conclude in one minute
                    // Don't know why, maybe bot prevention again?
                    // It seems it's an internal bug within .NET Selenium bindings
                    // https://stackoverflow.com/questions/22322596/selenium-error-the-http-request-to-the-remote-webdriver-timed-out-after-60-sec
                    {
                        //Console.WriteLine($"Web driver threw an exception on refresh, restarting the webdriver... [{DateTime.Now} local time]");
                        //RestartWebDriver(ref driver, options, username);
                        // What if we just let the driver refresh again? Would that solve this?
                    }
                }
            }
        }

        private static void OnProcessTerminated(object sender, EventArgs e)
        {
            WebdriverHandler.ShutdownDriver();
        }

        static void UpdateDataFile(ulong tweetID, string dataFile)
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

        static void SeparateUsernameandTweetID(string href, out string username, out ulong tweetID) // It's important that we separate username and Tweet ID so that we can deduce if it's a retweet etc.
        {
            const string twitter = "https://twitter.com/";
            const string status = "status/"; // I'm lazy to crank the counting to see how long these two are
            username = href.Substring(href.IndexOf(twitter) + twitter.Length);
            username = username.Substring(0, username.IndexOf("/"));
            tweetID = ulong.Parse(href.Substring(href.IndexOf(status) + status.Length));
        }
    }
}
