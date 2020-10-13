using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Streamnesia.CommandProcessing
{
    public static class Amnesia
    {
        private static Random Rng = new Random();
        const string LockFile = "../lock";
        const string CommandsFile = "../commands";
        static bool EchoSentCommands = false;

        public static Task DisplayTextAsync(string text)
        {
            if(string.IsNullOrWhiteSpace(text))
                return Task.CompletedTask;
            
            Sanitize(ref text);

            return SendToAmnesiaAsync($"SetMessageExact(\"{text}\", 0.0f)");
        }

        public static Task SetDeathHintTextAsync(string text)
        {
            if(string.IsNullOrWhiteSpace(text))
                return Task.CompletedTask;

            Sanitize(ref text);

            return SendToAmnesiaAsync($"SetDeathHint(\"StreamnesiaLiteral\", \"{text}\")");
        }

        public static Task ExecuteAsync(string angelcode)
        {
            Minify(ref angelcode);
            Format(ref angelcode);

            return SendToAmnesiaAsync(angelcode);
        }

        public static bool LastInstructionWasExecuted()
        {
            if(File.Exists(LockFile) && File.Exists(CommandsFile))
            {
                File.Delete(LockFile);
                return false;
            }

            return !File.Exists(LockFile) && !File.Exists(CommandsFile);
        }

        private static void Minify(ref string angelcode)
        {
            angelcode = Regex.Replace(angelcode, @"\t|\n|\r", "").Trim();
        }

        private static void Format(ref string angelcode)
        {
            angelcode = string.Format(angelcode, GenerateGuids());
            angelcode = angelcode.Replace("<<RANDOM_MUSIC>>", GetRandomOggMusic());
        }

        private static string[] GenerateGuids()
            => Enumerable.Range(0, 10).Select(i => Guid.NewGuid().ToString().Replace("-", string.Empty)).ToArray();

        private static async Task SendToAmnesiaAsync(string angelcode)
        {
            while(!LastInstructionWasExecuted())
            {
                await Task.Delay(100);
            }

            File.WriteAllText(LockFile, string.Empty);
            await Task.Delay(5);
            File.WriteAllText(CommandsFile, angelcode);
            File.Delete(LockFile);

            if(EchoSentCommands)
                Console.WriteLine(angelcode);
        }

        private static void Sanitize(ref string text)
        {
            text = Regex.Replace(text, @"\t|\n|\r", "").Trim();
            text = text.Replace(";", string.Empty).Replace("\\", string.Empty).Replace("\"", "\\\"");
        }

        private static string GetRandomOggMusic()
        {
            var music = new [] {
                "03_event_books.ogg",
                "29_event_end.ogg",
                "26_event_brute.ogg",
                "04_event_stairs.ogg",
                "24_event_vision02.ogg",
                "24_event_vision04.ogg",
                "00_event_gallery.ogg",
                "24_event_vision03.ogg",
                "21_event_pit.ogg",
                "27_event_bang.ogg",
                "19_event_brute.ogg",
                "15_event_prisoner.ogg",
                "05_event_falling.ogg",
                "26_event_agrippa_head.ogg",
                "05_event_steps.ogg",
                "00_event_hallway.ogg",
                "11_event_tree.ogg",
                "01_event_critters.ogg",
                "04_event_hole.ogg",
                "24_event_vision.ogg",
                "20_event_darkness.ogg",
                "15_event_elevator.ogg",
                "22_event_trapped.ogg",
                "15_event_girl_mother.ogg",
                "12_event_blood.ogg",
                "10_event_coming.ogg",
                "01_event_dust.ogg",
                "11_event_dog.ogg",
                "03_event_tomb.ogg"
            };

            return music[Rng.Next(0, music.Length)];
        }
    }
}
