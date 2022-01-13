using CommandLine;
using System.Collections.Generic;

namespace TwitterListener
{
    class Options
    {
        [Option('u', "username", Required = true, HelpText = "Twitter username that the program will listen to")]
        public string Username { get; set; }

        [Option('f', "firefox", HelpText = "States that the program will use Firefox webdriver", Default = false, Required = true, SetName = "Firefox")]
        public bool Firefox { get; set; }

        [Option('c', "chrome", HelpText = "States that the program will use Chrome webdriver", Default = false, Required = true, SetName = "Chrome")]
        public bool Chrome { get; set; }

        [Option('s', "sound", HelpText = "Sound file that will play when there is a new Tweet, needs to be .wav")]
        public string Sound { get; set; }

        [Option('p', "profile", HelpText = "States which profile user will use for browsing, if not invoked no profile will be used")]
        public string Profile { get; set; }

        //I hate Chrome so much for stealing the 'c' handle from this
        [Option('b', "clipboard", HelpText = "The program will copy the links to clipboard if this parameter is called, use it as --clipboard followed by \"tweet\", \"retweet\"" +
            "and/or \"space\" to state which situations you want links copied to your clipboard")]
        public IEnumerable<string> Clipboard { get; set; }
    }
}
