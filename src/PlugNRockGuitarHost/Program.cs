using PlugNRockGuitarHost.Effects;
using PlugNRockGuitarHost.Effects.NoteDetector;
using PlugNRockGuitarHost.Models;

namespace PlugNRockGuitarHost
{
    internal class Program
    {
        private static float volume = 1.0f;
        private static float balance = 0.0f;
        private static int semitones = 0;

        private static Dictionary<ConsoleKey, Action<ConsoleKey>> actions = new();

        private static void AddActions(IEnumerable<ConsoleKey> keys, Action<ConsoleKey> action)
        {
            foreach (ConsoleKey key in keys) 
                actions.TryAdd(key, action);
        }

        private static IProcessorEffect InitNoteDetector()
        {
            //BasicPitchNoteDetector detector = new(@"c:\2\basic pitch\nmp.onnx");
            NWavesNoteDetector detector = new();

            detector.NotesChanged += (object? sender, NotesChangedEventArgs e) => {
                Console.WriteLine($"{e.Time:T}: {{{string.Join(", ", e.MidiNotes.Select(INoteDetector.GetNoteName))}}}");
            };

            return detector;
        }

        private static IProcessorEffect InitPitchShifter()
        {
            PitchShifter shifter = new();

            Action<ConsoleKey> pitch = key => {
                if (key == ConsoleKey.OemPlus || key == ConsoleKey.Add)
                    semitones = Math.Min(++semitones, 12);
                else if (key == ConsoleKey.OemMinus || key == ConsoleKey.Subtract)
                    semitones = Math.Max(--semitones, -12);

                shifter.SetPitch(semitones);

                Console.WriteLine($"Pitch shift: {semitones}");
            };

            AddActions(new[] { ConsoleKey.OemPlus, ConsoleKey.Add, ConsoleKey.OemMinus, ConsoleKey.Subtract }, pitch);

            return shifter;
        }

        private static IProcessorEffect InitNAM()
        {
            NAM nam = new();
            nam.Init(new Dictionary<string, NAMDescriptor> {
                { "Dist", new() {
                    ModelPath = @"c:\2\nam\BOG UU II Gain BAL CAB.nam"
                }},
                { "Clean", new() {
                    ModelPath = @"c:\2\nam\TL LUSH CLEAN.nam",
                    //CabinetImpulse = @"c:\2\nam\Electrical Audio Centerfield Room MONO.wav",
                    OutputGain = 1.5f
                }},
            });
            nam.SetEffect("Dist");

            Action<ConsoleKey> changeEffect = key => {
                if (key == ConsoleKey.D1)
                    nam.SetEffect("Clean");
                if (key == ConsoleKey.D2)
                    nam.SetEffect("Dist");
            };

            AddActions(new[] { ConsoleKey.D1, ConsoleKey.D2 }, changeEffect);

            return nam;
        }

        private static IProcessorEffect InitVolumeControl()
        {
            VolumeControl control = new() {
                Balance = balance,
                Volume = volume
            };

            Action<ConsoleKey> changeVolume = key => {
                if (key == ConsoleKey.LeftArrow)
                    balance = Math.Max(balance - 0.1f, -1.0f);
                if (key == ConsoleKey.RightArrow)
                    balance = Math.Min(balance + 0.1f, 1.0f);

                if (key == ConsoleKey.UpArrow)
                    volume = Math.Min(volume + 0.1f, 1.0f);
                if (key == ConsoleKey.DownArrow)
                    volume = Math.Max(volume - 0.1f, -1.0f);

                control.Balance = balance;
                control.Volume = volume;

                Console.WriteLine($"Volume: {volume}, balance: {balance}");
            };

            AddActions(new [] { ConsoleKey.LeftArrow, ConsoleKey.RightArrow, ConsoleKey.UpArrow, ConsoleKey.DownArrow }, changeVolume);

            return control;
        }

        private static void KeyActions(ConsoleKey key)
        {
            if (actions.TryGetValue(key, out Action<ConsoleKey> action))
                action(key);
        }

        private static void Init()
        {
            GuitarProcessor processor = new();

            // Получаем список установленных ASIO-драйверов
            string[] asioDrivers = processor.GetDrivers();
            if (asioDrivers.Length == 0)
            {
                Console.WriteLine("ASIO-драйверы не найдены.");
                return;
            }

            // Выбираем нужный драйвер (например, ASIO4ALL или драйвер внешней звуковой карты)
            string driverName = "Focusrite USB ASIO";
            Console.WriteLine($"Используется драйвер: {driverName}");

            processor.Init(driverName);

            processor.AddEffect(InitNoteDetector());
            processor.AddEffect(InitPitchShifter());
            processor.AddEffect(InitNAM());
            processor.AddEffect(InitVolumeControl());

            processor.Start();
            Console.WriteLine("Управление: '+' громче, '-' тише, Enter — выход.");

            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                KeyActions(key);

                if (key == ConsoleKey.Enter) 
                    break;
            }

            processor.Stop();
        }

        [STAThread]
        static void Main(string[] args)
        {
            Init();
        }
    }
}
