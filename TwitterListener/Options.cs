using System;
using CommandLine;

namespace TwitterListener
{
    class Options
    {
        [Option('u', "username", Required = true, HelpText = "Twitter username that the program will listen to")]
        public string Username { get; set; }

        [Option('s', "sound", HelpText = "Sound file that will play when there is a new Tweet, needs to be .wav")]
        public string Sound { get; set; }
    }
}
