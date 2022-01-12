using System;
using CommandLine;

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
    }
}
