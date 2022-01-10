# TwitterListener
A simple Selenium C# program that alarms the user when a new Tweet (or retweet) is posted by the specified user
~~If you were somehow unlucky enough to clone this repository in the first place, be sure to clone it again after https://github.com/ApoxPaito/TwitterListener/commit/59b9f6b8b014a0a5e1f0af5d8084a635d7db9b4b because it wasn't even working properly before it, I'm a dumbass and didn't properly test or check my null refs/file modes~~
# Why not use Twitter API?
If Twitter API v1.1 wasn't a piece of garbage I would have used it by simply just fetching the RSS feed belonging to a certain username frequently. But they pretty much axed the RSS feeds with their new API, so let's say that led me into thinking of a "natural way" of fetching Tweets. Trust me, I've done my research on this. With Selenium, I can just keep refreshing pages belonging to anyone and not be subject to their ridiculous "25 requests max in 15 mins" restriction.
# So how do I use/install this?
Just run the TwitterListener in build via Command Prompt or w/e, if you run it without any args you'll be prompted with a help anyway, I'm too lazy to copypasta it here. Oh also, you will need Firefox installed for this because that's literally the webdriver we are using for it. You're more than welcome to change the code to Chrome and roll with it if you so wish though, I mean, can't really blame ya for wanting to kill your RAM.
# Can I play with this code since I think it sucks?
Sure, be my guest, fork it, modify it, twist it, do w/e your wild desires can achieve, I don't care if you use it to write a worm that DDoSes Twitter or if you don't even credit me in your built-from-this code.
