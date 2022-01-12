using System;
using System.IO;
using System.Media;

namespace TwitterListener
{
    class Alarm
    {
        private SoundPlayer player;

        public Alarm()
        {
            player = null;
        }

        public bool LoadSoundFile(string path)
        {
            if (path == null)
            {
                Console.WriteLine("No alarm sound was identified, still moving on");
                return true;
            }
            if (!path.Contains(".wav"))
            {
                Console.WriteLine("Sound file isn't a .wav file, ignoring");
                return true;
            }
            try
            {
                FileStream fs = new FileStream(path, FileMode.Open);
                player = new SoundPlayer(fs);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Specified file cannot be found in path, press Q to halt operation or any other key to continue without an alarm sound");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.Q)
                    return false;
                player = null;
            }
            return true;
        }

        public void Play()
        {
            if (player != null)
                player.Play();
        }
    }
}
