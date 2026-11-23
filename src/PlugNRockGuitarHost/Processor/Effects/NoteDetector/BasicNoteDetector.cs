using System.Collections.Concurrent;

namespace PlugNRockGuitarHost.Processor.Effects.NoteDetector
{
    public abstract class BasicNoteDetector : BasicEffect, INoteDetector
    {
        #region Поля

        protected ConcurrentQueue<float> ringBuffer = new();
        protected CancellationTokenSource tokenSource;

        #endregion Поля

        #region Свойства

        public List<int> MidiNotes { get; protected set; } = new();

        public List<string> Notes => MidiNotes.Select(INoteDetector.GetNoteName).ToList();

        public float NoiseGateThreshold { get; set; } = DbToLinear(-80f);

        #endregion Свойства

        #region События

        public event EventHandler<NotesChangedEventArgs>? NotesChanged;

        #endregion События

        #region Вспомогательные функции

        protected static float DbToLinear(float db) => (float)Math.Pow(10.0, db / 20.0);

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
            if (window.Length == 0)
                return true;

            // Проверка громкости (Noise Gate)
            bool isSilence = (float)Math.Sqrt(window.Average(x => x * x)) < NoiseGateThreshold;

            // Очистка списка нот при наступлении тишины
            if (isSilence && MidiNotes.Count > 0)
                ChangeNotes(new List<int>());

            return isSilence;
        }

        #endregion Вспомогательные функции

        #region Основные функции

        protected void ChangeNotes(List<int> notes)
        {
            MidiNotes = notes;
            NotesChanged?.Invoke(this, new NotesChangedEventArgs(MidiNotes));
        }

        #endregion Основные функции

        #region IDisposable

        public override void Dispose()
        {
            tokenSource.Cancel(false);
        }

        #endregion IDisposable
    }
}