using System;
using System.IO;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace TwitterListener
{
    static class WebdriverHandler
    {
        public enum Browser
        {
            Firefox,
            Chrome
        }

        public static WebDriver Init(Browser browser, string profileName)
        {;
            switch (browser)
            {
                case Browser.Firefox:
                    return StartFirefoxDriver(profileName);
                case Browser.Chrome:
                    return StartChromeDriver(profileName);
            }
            return null;
        }

        public static void RefreshPageWithExceptionHandling(WebDriver driver)
        {
            try
            {
                driver.Navigate().Refresh();
            }
            catch (WebDriverTimeoutException)
            { }
            catch (WebDriverException)
            { }
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
