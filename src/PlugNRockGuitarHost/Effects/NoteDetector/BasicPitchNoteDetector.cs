using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Runtime.InteropServices;

namespace PlugNRockGuitarHost.Effects.NoteDetector
{
    public class BasicPitchNoteDetector : BasicNoteDetector
    {
        #region Поля

        private const int targetSampleRate = 22050;
        private const int windowSize = 43844; // Окно Basic Pitch (~1.988 сек)
        private const int hopSize = 11025;    // Шаг сдвига окна (~0.5 сек для работы в реальном времени)

        private InferenceSession session;

        private BufferedWaveProvider bufferedProvider;
        private WdlResamplingSampleProvider resampler;
        private float[] resampledChunk = new float[1024];

        #endregion Поля

        #region Вспомогательные функции

        private void DetectNote(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
        {
            // 1. Берем тензоры нот и атак по имени (размерность каждого:)
            Tensor<float> noteTensor = results.First(r => r.Name == "StatefulPartitionedCall:1").AsTensor<float>();
            Tensor<float> onsetTensor = results.First(r => r.Name == "StatefulPartitionedCall:2").AsTensor<float>();

            int frames = noteTensor.Dimensions[1];  // 172 временных шага
            int pitches = noteTensor.Dimensions[2]; // 88 нот (MIDI 21..108)

            float frameThreshold = 0.35f; // Порог удержания ноты
            float onsetThreshold = 0.50f; // Порог начала (атаки) ноты

            // 2. Проходим по временным фреймам
            HashSet<int> notes = new();
            for (int f = 0; f < frames; f++)
                for (int p = 0; p < pitches; p++)
                {
                    float noteProb = noteTensor[0, f, p];
                    float onsetProb = onsetTensor[0, f, p];

                    // Если вероятность ноты и её атаки выше порога — нота сыграна
                    if (noteProb >= frameThreshold && onsetProb >= onsetThreshold)
                    {
                        int midiNote = p + 21; // Индекс 0 соответствует ноте A0 (MIDI 21)

                        notes.Add(midiNote);
                    }
                }

            ChangeNotes(notes.ToList());
        }

        private async Task PredictNote()
        {
            float[] slidingWindow = new float[windowSize];

            while (!tokenSource.Token.IsCancellationRequested)
            {
                if (ringBuffer.Count >= hopSize)
                {
                    // Сдвигаем старые сэмплы влево и дописываем новые 0.5 сек
                    SlideWindow(slidingWindow, hopSize);

                    if (!NoiseGate(slidingWindow)){

                        // Формируем входной тензор (размерность зависит от конкретной модели, для CREPE обычно)
                        DenseTensor<float> inputTensor = new(slidingWindow, new[] { 1, windowSize, 1 });
                        List<NamedOnnxValue> inputs = new() {
                            NamedOnnxValue.CreateFromTensor("serving_default_input_2:0", inputTensor) // Имя входа уточните через Netron
                        };

                        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
                        DetectNote(results);
                    }
                }

                await Task.Delay(10);
            }
        }

        #endregion Вспомогательные функции

        #region Основные функции

        public BasicPitchNoteDetector(string modelPath)
        {
            session = new InferenceSession(modelPath);

            // Настройка ресемплера NAudio (48 000 Гц -> 22 050 Гц, Mono)
            bufferedProvider = new(WaveFormat.CreateIeeeFloatWaveFormat(Consts.SampleRate, 1)) {
                DiscardOnBufferOverflow = true
            };
            resampler = new(bufferedProvider.ToSampleProvider(), targetSampleRate);

            tokenSource = new();

            Task.Run(PredictNote, tokenSource.Token);
        }

        public override void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right)
        {
            bufferedProvider.AddSamples(MemoryMarshal.AsBytes(input));
            int read = resampler.Read(resampledChunk);
            for (int i = 0; i < read; i++)
                ringBuffer.Enqueue(resampledChunk[i]);
        }

        #endregion Основные функции

        #region IDisposable

        public override void Dispose()
        {
            base.Dispose();

            session.Dispose();
        }

        #endregion IDisposable
    }
}