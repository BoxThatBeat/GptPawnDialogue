using System.Collections.Concurrent;

namespace GptPawnDialogue
{
    public static class Logger
    {
        class Msg
        {
            internal string txt;
            internal int level;
        }

        private static readonly ConcurrentQueue<Msg> log = new();

        public static void Message(string txt)
        {
            log.Enqueue(new Msg() { txt = txt, level = 0 });
        }

        public static void Warning(string txt)
        {
            log.Enqueue(new Msg() { txt = txt, level = 1 });
        }

        public static void Error(string txt)
        {
            log.Enqueue(new Msg() { txt = txt, level = 2 });
        }

        private static string PrefixMessage(string message) => $"[{Mod.Name} v{Mod.Version}] {message}";

        internal static void Log()
        {
            while (log.Count > 0 && Mod.Running)
            {
                if (log.TryDequeue(out var msg) == false)
                    continue;
                switch (msg.level)
                {
                    case 0:
                        Verse.Log.Message(PrefixMessage(msg.txt));
                        break;
                    case 1:
                        Verse.Log.Warning(PrefixMessage(msg.txt));
                        break;
                    case 2:
                        Verse.Log.Error(PrefixMessage(msg.txt));
                        break;
                }
            }
        }
    }
}
