using System;

using OpenQA.Selenium;

namespace TwitterListener
{
    static class Listener
    {
        private const string relativeFirst = "/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/section/div/div" +
            "/div[1]/div/div/div/article/div/div/div/div[2]/div[2]/div[1]/div/div/div[1]"; // This is the XPath of first relative Tweet in the feed
        private const string relativeSecond = "/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/section/div/div" +
            "/div[2]/div/div/div/article/div/div/div/div[2]/div[2]/div[1]/div/div/div[1]"; // This is the XPath of second relative Tweet in the feed
        private const string relativeThird = "/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/section/div/div" +
            "/div[3]/div/div/div/article/div/div/div/div[2]/div[2]/div[1]/div/div/div[1]"; // This is the XPath of third relative Tweet in the feed
        // All are valid for now ofc

        public static IWebElement GetNewestTweetElement(ref WebDriver driver, WebdriverHandler.Browser browser, string profileName, ref bool exitState, string username)
        {
            while (!exitState)
            {
                try
                {
                    IWebElement element = driver.FindElement(By.XPath("/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/section/div/div/div[1]/div/div/div/article/div/div/div/div[1]"));
                    // Get a grab of relative path of the retweet and pin data elements
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0); // Remove delay for a millisec
                    if (element.FindElements(By.XPath(".//*")).Count == 0) // See if any children exist
                        return driver.FindElement(By.XPath(relativeFirst)); // This is a direct Tweet, no retweet or pin data
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10); // A delay of ten secs till exception is thrown when looking for element
                    element = element.FindElement(By.XPath(".//div/div/div/div/div[2]/div/div")).FindElement(By.TagName("div")); // Crawl deeper in the HTML and close into data-testid block
                    // Pinned Tweets are the bane of my existence I swear to 75 varieties of a butterfly
                    if (!string.IsNullOrEmpty(element.GetAttribute("data-testid"))) // Pinned Tweets have this attribute called data-testid with socialContext in it,
                                                                                    // retweets don't have it and this will return an empty string on those
                    {
                        // Stop scuffing your ad Tweets beyond usage Twitter, holy fucking shit
                        // Check if there is an empty ad shell on the second slot for some bloody reason
                        const string relativeAd = "/html/body/div[1]/div/div/div[2]/main/div/div/div/div[1]/div/div[2]/div/div/section/div/div/div[2]/div";
                        element = element.FindElement(By.XPath(relativeAd));
                        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0); // Remove delay for a millisec
                        if (element.FindElements(By.XPath(".//*")).Count == 0) // If it has no children, this is a stinky empty ad Tweet which has no reason to be there for some bloody unapparent reason
                        {
                            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10); // Let's not forget to reintroduce implicit wait
                            return driver.FindElement(By.XPath(relativeThird));
                        }
                        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10); // A delay of ten secs till exception is thrown when looking for element
                        // If not, we'll check the second Tweet
                        element = element.FindElement(By.XPath(relativeSecond));
                        if (TryCatchForAdTweet(element)) // But there is a chance that second Tweet will be an ad even if we don't get the empty ad Tweet scuff
                            return driver.FindElement(By.XPath(relativeThird)); // Let's fetch the next Tweet on the line
                        return element; // If even this fails then Twitter is bloody hopeless
                    }
                    return driver.FindElement(By.XPath(relativeFirst)); // Topmost re/Tweet is good to go
                }
                catch (Exception ex) // If for some reason it can't find the element, it will throw this
                {
                    if (ex is NoSuchElementException || ex is StaleElementReferenceException) WebdriverHandler.RefreshPageWithExceptionHandling(driver);
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

        private static bool TryCatchForAdTweet(IWebElement element)
        {
            try
            {
                IWebElement sacrifice = element.FindElements(By.TagName("a"))[1];
                // This will throw an exception if this is a stinky ad
            }
            catch
            {
                return true;
            }
            return false;
        }
    }
}
