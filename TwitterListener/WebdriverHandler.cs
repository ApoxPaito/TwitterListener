using System;
using System.IO;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace TwitterListener
{
    static class WebdriverHandler
    {
        public enum Type
        {
            Firefox,
            Chrome
        }

        static WebDriver driver = null;

        public static WebDriver Init(Type type, string profileName)
        {
            switch (type)
            {
                case Type.Firefox:
                    driver = StartFirefoxDriver(profileName);
                    return driver;
                case Type.Chrome:
                    driver = StartChromeDriver(profileName);
                    return driver;
            }
            return null;
        }

        public static void RestartDriver(ref WebDriver driver, Type type, string username, string profileName)
        {
            driver.Quit();
            switch (type)
            {
                case Type.Firefox:
                    driver = StartFirefoxDriver(profileName);
                    WebdriverHandler.driver = driver;
                    break;
                case Type.Chrome:
                    driver = StartChromeDriver(profileName);
                    WebdriverHandler.driver = driver;
                    break;
            }
            while (true)
            {
                try
                {
                    driver.Navigate().GoToUrl(new Uri($"https://twitter.com/{username}")); // Sometimes even this throws an exception, so let's be safe here
                    // I don't like my code throwing unhandled exceptions at my face
                    break;
                }
                catch (WebDriverException)
                {
                    //RestartWebDriver(ref driver, options, username);
                }
            }
        }

        public static void ShutdownDriver()
        {
            driver.Quit();
        }

        private static FirefoxDriver StartFirefoxDriver(string profileName)
        {
            FirefoxOptions fOptions = new FirefoxOptions();
            fOptions.AddArgument("-headless");
            if (!string.IsNullOrEmpty(profileName))
            {
                FirefoxProfileManager manager = new FirefoxProfileManager();
                FirefoxProfile fProfile = manager.GetProfile(profileName);
                fOptions.Profile = fProfile;
            }
            else // No point in asking for profile if we always private
                fOptions.AddArgument("-private"); 
            return new FirefoxDriver(fOptions);
        }

        private static ChromeDriver StartChromeDriver(string profileName)
        {
            ChromeOptions cOptions = new ChromeOptions();
            cOptions.AddArgument("--headless");
            if (!string.IsNullOrEmpty(profileName))
            {
                //Look at how much work I'm doing just for this garbage piece of a browser, this is also a wake up call for you to drop using bad programs
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Google\\Chrome\\User Data");
                cOptions.AddArguments($"--user-data-dir={path}", $"--profile-directory={profileName}");
                //I know it's literally two lines of code but I don't feel this is fail safe at all
            }
            else // No point in asking for profile if we always private
                cOptions.AddArgument("--incognito");
            return new ChromeDriver(cOptions);
        }
    }
}
