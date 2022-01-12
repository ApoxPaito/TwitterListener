using System;

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

        public static WebDriver Init(Type type)
        {
            switch (type)
            {
                case Type.Firefox:
                    FirefoxOptions fOptions = new FirefoxOptions();
                    fOptions.AddArguments("-headless", "-private");
                    //FirefoxProfileManager manager = new FirefoxProfileManager();
                    //FirefoxProfile fProfile = manager.GetProfile("default");
                    //fOptions.Profile = fProfile;
                    driver = new FirefoxDriver(fOptions);
                    return driver;
                case Type.Chrome:
                    ChromeOptions cOptions = new ChromeOptions();
                    cOptions.AddArguments("--headless", "--incognito");
                    driver = new ChromeDriver(cOptions);
                    return driver;
            }
            return null;
        }

        public static void RestartDriver(ref WebDriver driver, Type type, string username)
        {
            driver.Quit();
            switch (type)
            {
                case Type.Firefox:
                    FirefoxOptions fOptions = new FirefoxOptions();
                    fOptions.AddArguments("-headless", "-private");
                    driver = new FirefoxDriver(fOptions);
                    WebdriverHandler.driver = driver;
                    break;
                case Type.Chrome:
                    ChromeOptions cOptions = new ChromeOptions();
                    cOptions.AddArguments("-headless", "-private");
                    driver = new ChromeDriver(cOptions);
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
    }
}
