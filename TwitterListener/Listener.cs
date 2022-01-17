using System;

using OpenQA.Selenium;

namespace TwitterListener
{
    static class Listener
    {
        private const string d = "M20.235 14.61c-.375-1.745-2.342-3.506-4.01-4.125l-.544-4.948 1.495-2.242c.157-.236.172-." +
            "538.037-.787-.134-.25-.392-.403-.675-.403h-9.14c-.284 0-.542.154-.676.403-.134.25-.12.553.038.788l1.498 2.247-.484 4." +
            "943c-1.668.62-3.633 2.38-4.004 4.116-.04.16-.016.404.132.594.103.132.304.29.68.29H8.64l2.904 6.712c.078.184.26.302.458." +
            "302s.38-.118.46-.302l2.903-6.713h4.057c.376 0 .576-.156.68-.286.146-.188.172-.434.135-.59z";
        // Apparently this is the value that svg path holds in everyone, probably some encoded pbs.twimg url or smth
        private const string relativeFirst = "/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/div[2]/section/div/div" +
            "/div[1]/div/div/article/div/div/div/div[2]/div[2]/div[1]/div/div/div[1]"; // This is the XPath of first relative Tweet in the feed
        private const string relativeSecond = "/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/div[2]/section/div/div" +
            "/div[3]/div/div/article/div/div/div/div[2]/div[2]/div[1]/div/div/div[1]"; // This is the XPath of second relative Tweet in the feed
        // Both are valid for now ofc

        public static IWebElement GetNewestTweetElement(WebDriver driver, ref bool exitState)
        {
            while (!exitState)
            {
                try
                {
                    IWebElement element = driver.FindElement(By.XPath("/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/div[2]/section/div/div/div[1]/div/div/article/div/div/div/div[1]/div/div"));
                    // Get a grab of relative path of the retweet and pin data elements
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0); // Remove delay for a millisec
                    if (element.FindElements(By.XPath(".//*")).Count == 0) // See if any children exist
                        return driver.FindElement(By.XPath(relativeFirst)); // This is a direct Tweet, no retweet or pin data
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10); // A delay of ten secs till exception is thrown when looking for element
                    element = element.FindElement(By.XPath(".//div/div/div[2]/div/div")).FindElement(By.TagName("div")); // Crawl deeper in the HTML and close into data-testid block
                    // Pinned Tweets are the bane of my existence I swear to 75 varieties of a butterfly
                    if (!string.IsNullOrEmpty(element.GetAttribute("data-testid"))) // Pinned Tweets have this attribute called data-testid with socialContext in it,
                        // retweets don't have it and this will return an empty string on those
                        return driver.FindElement(By.XPath(relativeSecond)); // Let's fetch the next Tweet on the line
                    return driver.FindElement(By.XPath(relativeFirst)); // Topmost re/Tweet is good to go
                }
                catch (NoSuchElementException) // If for some reason it can't find the element, it will throw this
                {
                    // What if we just deleted cookies and rerolled?
                    driver.Manage().Cookies.DeleteAllCookies();
                    WebdriverHandler.RefreshPageWithExceptionHandling(driver);
                }
            }
            return null;
        }

        private static bool TryCatchForPinnedTweet(IWebElement element)
        {
            try
            {
                element.GetAttribute("data-testid").Equals("socialContext");
            }
            catch (NullReferenceException)
            {
                return false;
            }
            return true;
        }
    }
}
