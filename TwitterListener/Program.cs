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
            ulong lastTweetID = 0;
            SoundPlayer player = null;
            string soundPath = parsed.Value.Sound;
            string dataFile = $"{username}.json";

            // Init sound file
            if (soundPath == null)
                Console.WriteLine("No alarm sound was identified, still moving on");
            else if (!soundPath.Contains(".wav"))
                Console.WriteLine("Sound file isn't a .wav file, ignoring");
            else
            {
                try
                {
                    FileStream fs = new FileStream(soundPath, FileMode.Open);
                    player = new SoundPlayer(fs);
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Specified file cannot be found in path, press Q to halt operation or any other key to continue without an alarm sound");
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Q)
                        return;
                    player = null;
                }
            }

            // Check for {username}.json and create if it doesn't exist
            try
            {
                FileStream fs = new FileStream(dataFile, FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                string jsonString = sr.ReadToEnd();
                JObject jo = JObject.Parse(jsonString);
                try
                {
                    lastTweetID = jo["LastTweetID"].ToObject<ulong>();
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
            FirefoxOptions options = new FirefoxOptions();
            options.AddArguments("-private", "-headless");
            IWebDriver driver = new FirefoxDriver(options);
            driver.Navigate().GoToUrl(new Uri($"https://twitter.com/{username}")); // Navigate to said page

            // *** Operation under way ***
            while (true)
            {
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
                        Console.WriteLine($"Web driver threw an exception on refresh, restarting the webdriver... [{DateTime.Now} local time]");
                        RestartWebDriver(ref driver, options, username);
                    }
                }
                IWebElement element;
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (true) // Keep trying to fetch the last Tweet element till it gets loaded up
                {
                    try
                    {
                        element = driver.FindElement(By.XPath("/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/div[2]/section/div/div/div[1]/div/div/article/div/div/div/div[2]/div[2]/div[1]/div/div/div[1]"));
                        break;
                    }
                    catch (NoSuchElementException) // If the page hasn't loaded up yet, it will throw this
                    {
                        if (sw.ElapsedMilliseconds > 10000) // We have been botted, let's restart the webdriver
                        {
                            sw.Reset();
                            Console.WriteLine($"Failed to get Tweet data, Twitter probably got up to our jig, restarting the webdriver... [{DateTime.Now} local time]");
                            driver.Manage().Cookies.DeleteAllCookies();
                            RestartWebDriver(ref driver, options, username);
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
                    Console.WriteLine($"New Retweet from {username} at {DateTime.Now} local time");
                    player.Play();
                    UpdateDataFile(tweetID, dataFile);
                }
                else if (tweetID < lastTweetID) // This is very unlikely to happen but it means they've actually deleted their last Tweet
                {
                    lastTweetID = tweetID;
                    Console.WriteLine($"Last Tweet from {username} has a lower ID than stored, did they delete something? [{DateTime.Now} local time]");
                    //player.Play(); // Don't warn when Tweets deleted?
                    UpdateDataFile(tweetID, dataFile);
                }
                else if (tweetID != lastTweetID) // Otherwise, it's a direct tweet
                {
                    lastTweetID = tweetID;
                    Console.WriteLine($"New Tweet from {username} at {DateTime.Now} local time");
                    player.Play();
                    UpdateDataFile(tweetID, dataFile);
                }
            }
        }

        static void UpdateDataFile(ulong tweetID, string dataFile)
        {
            Dictionary<string, ulong> jsonDict = new Dictionary<string, ulong>
            {
                {"LastTweetID", tweetID }
            };

            string jsonString = JsonConvert.SerializeObject(jsonDict, Formatting.Indented);
            FileStream fs = new FileStream(dataFile, FileMode.Truncate);
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

        static void RestartWebDriver(ref IWebDriver driver, FirefoxOptions options, string username)
        {
            driver.Quit();
            driver = new FirefoxDriver(options);
            driver.Navigate().GoToUrl(new Uri($"https://twitter.com/{username}"));
        }
    }
}
