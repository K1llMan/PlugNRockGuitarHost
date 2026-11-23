using NWaves.Features;
using NWaves.Transforms;
using NWaves.Windows;

namespace PlugNRockGuitarHost.Processor.Effects.NoteDetector
{
    public class NWavesNoteDetector : BasicNoteDetector
    {
        #region Поля

        private readonly int bufferSize = 4096;

        private readonly float low = 60f;        // Минимальная частота (Гц)
        private readonly float high = 1500f;     // Максимальная частота (Гц)

        private readonly RealFft fft;
        private readonly int fftSize = 8192;     // Размер БПФ (Zero-Padding для точности)
        private readonly float[] fftWindow;

        #endregion Поля

        #region Вспомогательные функции

        #region Одна нота

        private void DetectNote(float[] window)
        {
            // Вызываем статический метод Pitch.FromYin напрямую для массива сэмплов
            float frequency = Pitch.FromYin(
                samples: window,
                samplingRate: Consts.SampleRate,
                low: low,        // Минимальная частота (Гц)
                high: high,     // Максимальная частота (Гц)
                cmdfThreshold: 0.15f // Порог уверенности (меньше = строже)
            );

            // Если алгоритм не уверен или тишина, он возвращает 0
            if (frequency > 0)
                ChangeNotes(new() {
                    (int)Math.Round(69 + 12 * Math.Log2(frequency / 440.0))
                });
        }

        #endregion Одна нота

        #region Много нот

        private float InterpolatePeak(float[] spectrum, int bin)
        {
            float alpha = spectrum[bin - 1];
            float beta = spectrum[bin];
            float gamma = spectrum[bin + 1];
            float denominator = alpha - 2 * beta + gamma;

            if (Math.Abs(denominator) < 1e-6f)
                return bin;

            return bin + 0.5f * (alpha - gamma) / denominator;
        }

        private void DetectNotes(float[] window)
        {
            // 2. Применяем оконную функцию и дополняем нулями до fftSize
            float[] paddedFrame = new float[fftSize];
            for (int i = 0; i < bufferSize; i++)
                paddedFrame[i] = fftWindow[i] * window[i];

            // 3. Вычисляем спектр магнитуд
            float[] spectrum = new float[fftSize / 2 + 1];
            fft.MagnitudeSpectrum(paddedFrame, spectrum);

            // Ограничиваем диапазон гитары
            float binResolution = (float)Consts.SampleRate / fftSize;
            int minBin = (int)(low / binResolution);
            int maxBin = (int)(high / binResolution);

            // Находим максимальный пик для относительного порога
            float maxMagnitude = 0f;
            for (int i = minBin; i <= maxBin; i++)
                if (spectrum[i] > maxMagnitude) 
                    maxMagnitude = spectrum[i];

            if (maxMagnitude < 0.5f) 
                return;

            HashSet<int> detectedMidiNotes = new();
            float threshold = maxMagnitude * 0.22f; // Отсекаем тихие призвуки (22% от главного пика)

            // 4. Итеративно ищем до 6 нот (по числу струн)
            for (int iter = 0; iter < 6; iter++)
            {
                int peakBin = -1;
                float peakVal = threshold;

                for (int i = minBin; i <= maxBin; i++)
                    if (spectrum[i] > peakVal && spectrum[i] > spectrum[i - 1] && spectrum[i] > spectrum[i + 1])
                    {
                        peakVal = spectrum[i];
                        peakBin = i;
                    }

                if (peakBin == -1) 
                    break; // Больше значимых пиков нет

                // Параболическая интерполяция для точной частоты
                float exactBin = InterpolatePeak(spectrum, peakBin);
                float frequency = exactBin * binResolution;

                int midiNote = (int)Math.Round(69 + 12 * Math.Log2(frequency / 440.0));
                detectedMidiNotes.Add(midiNote);

                // 5. Вычитаем/подавляем гармоники найденной ноты (1-я основная и обертоны до 6-го)
                for (int harmonic = 1; harmonic <= 6; harmonic++)
                {
                    float harmonicFreq = frequency * harmonic;
                    int centerBin = (int)Math.Round(harmonicFreq / binResolution);

                    // Ширина вырезаемого окна зависит от номера гармоники (учитываем негармоничность струн)
                    int radius = Math.Max(3, harmonic * 2);

                    for (int b = centerBin - radius; b <= centerBin + radius; b++)
                        if (b >= 0 && b < spectrum.Length)
                        {
                            // Уменьшаем энергию гармоники, а не обнуляем полностью (на случай октавных аккордов)
                            spectrum[b] *= harmonic == 1 ? 0.0f : 0.15f;
                        }
                }
            }

            ChangeNotes(detectedMidiNotes.ToList());
        }

        #endregion Много нот

        #endregion Вспомогательные функции

        #region Основные функции

        private async Task PredictNotes(Action<float[]> detector)
        {
            float[] slidingWindow = new float[bufferSize];

            while (!tokenSource.Token.IsCancellationRequested)
            {
                if (ringBuffer.Count >= bufferSize)
                {
                    SlideWindow(slidingWindow, bufferSize / 2);

                    if (!NoiseGate(slidingWindow))
                        detector(slidingWindow);
                }

                await Task.Delay(10);
            }
        }

        public NWavesNoteDetector(bool singleNote = false)
        {
            fft = new RealFft(fftSize);
            fftWindow = Window.Blackman(bufferSize);

            tokenSource = new();
            Task.Run(() => 
                PredictNotes(singleNote ? DetectNote : DetectNotes), 
                tokenSource.Token
            );
        }

        public override void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right)
        {
            foreach (float sample in input)
                ringBuffer.Enqueue(sample);
        }

        #endregion Основные функции
    }
}