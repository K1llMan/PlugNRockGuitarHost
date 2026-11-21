using NAudio.Wave;

using PlugNRockGuitarHost.Effects;

namespace PlugNRockGuitarHost
{
    public class GuitarProcessor: IDisposable
    {
        #region Поля

        private List<IProcessorEffect> effects = new();

        #endregion Поля

        #region Свойства

        protected AsioDevice Device {  get; set; }

        #endregion Свойства

        #region Вспомогательные функции

        private void Processor(in AsioProcessBuffers buffers)
        {
            // Получаем сэмплы с гитары (индекс 0 соответствует первому элементу InputChannels)
            ReadOnlySpan<float> input = buffers.GetInput(0);

            // Получаем буферы левого и правого каналов наушников
            Span<float> left = buffers.GetOutput(0);
            Span<float> right = buffers.GetOutput(1);

            // Работа идёт через буфер левого канала
            input.CopyTo(left);
            input.CopyTo(right);

            foreach (IProcessorEffect effect in effects)
                effect.Process(input, left, right);
        }

        #endregion Вспомогательные функции

        #region Основные функции

        public string[] GetDrivers()
        {
            return AsioDevice.GetDriverNames();
        }

        public void Init(string driverName)
        {
            Device = AsioDevice.Open(driverName);

            Device.InitDuplex(new AsioDuplexOptions
            {
                // Индекс физического входа (0 = Input 1, 1 = Input 2)
                InputChannels = [0],

                // Индексы выходов для наушников (обычно 0 и 1 — левый и правый каналы)
                OutputChannels = [0, 1],

                SampleRate = Consts.SampleRate, // или 44100 в зависимости от настроек карты

                Processor = Processor
            });
        }

        public void Start()
        {
            Device.Start();
        }

        public void Stop()
        {
            Device.Stop();
        }

        public void AddEffect(IProcessorEffect effect)
        {
            effects.Add(effect);
        }

        public void AddEffects(IEnumerable<IProcessorEffect> effect)
        {
            effects.AddRange(effect);
        }

        public void Clear()
        {
            foreach (IProcessorEffect effect in effects)
                effect.Dispose();

            effects.Clear();
        }

        #endregion Основные функции

        #region IDisposable

        public void Dispose()
        {
            Clear();
        }

        #endregion IDisposable
    }
}
