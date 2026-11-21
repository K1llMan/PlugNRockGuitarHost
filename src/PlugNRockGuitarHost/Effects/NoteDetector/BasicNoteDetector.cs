using System.Collections.Concurrent;

namespace PlugNRockGuitarHost.Effects.NoteDetector
{
    public abstract class BasicNoteDetector : INoteDetector
    {
        #region Поля

        protected ConcurrentQueue<float> ringBuffer = new();
        protected CancellationTokenSource tokenSource;

        #endregion Поля

        #region Свойства

        public List<int> MidiNotes { get; protected set; } = new();

        public List<string> Notes => MidiNotes.Select(INoteDetector.GetNoteName).ToList();

        #endregion Свойства

        #region События

        public event EventHandler<NotesChangedEventArgs>? NotesChanged;

        #endregion События

        #region Вспомогательные функции

        protected void SlideWindow(in float[] slidingWindow, int slideSize)
        {
            int length = slidingWindow.Length - slideSize;

            // Сдвигаем старые сэмплы влево и дописываем новые 0.5 сек
            Array.Copy(slidingWindow, slideSize, slidingWindow, 0, length);
            for (int i = length; i < slidingWindow.Length; i++)
                ringBuffer.TryDequeue(out slidingWindow[i]);
        }

        protected bool NoiseGate(float[] window)
        {
            // Проверка громкости (Noise Gate)
            float rms = (float)Math.Sqrt(window.Average(x => x * x));
            bool isSilence = rms < 0.005f;

            // Очистка списка нот при наступлении тишины
            if (isSilence && MidiNotes.Count > 0)
                ChangeNotes(new List<int>());

            return isSilence;
        }

        #endregion Вспомогательные функции

        #region Основные функции

        public virtual void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right)
        {
            throw new NotImplementedException();
        }

        protected void ChangeNotes(List<int> notes)
        {
            MidiNotes = notes;
            NotesChanged?.Invoke(this, new NotesChangedEventArgs(MidiNotes));
        }

        #endregion Основные функции


        #region IDisposable

        public virtual void Dispose()
        {
            tokenSource.Cancel(false);
        }

        #endregion IDisposable
    }
}