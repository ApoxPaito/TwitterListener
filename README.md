# TwitterListener
A simple Selenium C# program that alarms the user when a new Tweet (or retweet) is posted by the specified user
# Why not use Twitter API?
If Twitter API v1.1 wasn't a piece of garbage I would have used it by simply just fetching the RSS feed belonging to a certain username frequently. But they pretty much axed the RSS feeds with their new API, so let's say that led me into thinking of a "natural way" of fetching Tweets. Trust me, I've done my research on this. With Selenium, I can just keep refreshing pages belonging to anyone and not be subject to their ridiculous "25 requests max in 15 mins" restriction.
# So how do I use/install this?
Just run the TwitterListener in build via Command Prompt or w/e, if you run it without any args you'll be prompted with a help anyway, I'm too lazy to copypasta it here. Oh also, you will need Firefox or Chrome installed for this because those are literally the webdrivers we are using for it.

If you want to listen to Spaces like I do, you'll need to create a certain profile for Selenium to use and log into your Twitter on it. Simply Google "creating a Firefox (or Chrome for you helpless lots) profile" on how to do it, I'm also lazy to type it down here when there are guides out there that can describe it better than I can. But anyway, on default the listener doesn't use any profile.
# Can I play with this code since I think it sucks?
Sure, be my guest, fork it, modify it, twist it, do w/e your wild desires can achieve, I don't care if you use it to write a worm that DDoSes Twitter or if you don't even credit me in your built-from-this code.
