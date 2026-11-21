using RubberBandSharp;

namespace PlugNRockGuitarHost.Effects
{
    public class PitchShifter : IProcessorEffect
    {
        #region Поля

        private readonly RubberBandStretcherMono stretcher;
        private int pitchSemitones = 0;

        #endregion Поля


        #region Основные функции

        public PitchShifter()
        {
            // Настройка для работы в реальном времени
            RubberBandStretcher.Options options = 
                RubberBandStretcher.Options.ProcessRealTime |
                RubberBandStretcher.Options.PitchHighConsistency |
                RubberBandStretcher.Options.PitchHighQuality;

            stretcher = new RubberBandStretcherMono(
                Consts.SampleRate,
                options,
                initialTimeRatio: 1.0,  // Темп не меняем
                initialPitchScale: 1.0
            );
        }

        public void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right)
        {
            stretcher.Process(left, (uint)input.Length, false);

            int available = stretcher.Available();
            if (available == 0)
                return;

            // Не забираем больше, чем вмещают выходные буферы
            int toRetrieve = Math.Min(available, left.Length);

            // Забираем результат сразу в первый выходной буфер
            stretcher.Retrieve(left, (uint)toRetrieve);

            left.CopyTo(right);
        }

        public void SetPitch(int semitones)
        {
            pitchSemitones = semitones;

            stretcher.SetPitchScale(Math.Pow(2.0, pitchSemitones / 12.0));
        }

        #endregion Основные функции

        #region IDisposable

        public void Dispose()
        {
        }

        #endregion IDisposable
    }
}