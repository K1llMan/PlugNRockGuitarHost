using NAudio.Effects;
using NAudio.Nam;
using NAudio.Wave;

using PlugNRockGuitarHost.Models;

namespace PlugNRockGuitarHost.Effects
{
    public class NAM : IProcessorEffect
    {
        #region Поля

        // Целевой уровень громкости (в стандартном плагине NAM обычно -18.0 дБ)
        private const float DefaultTargetLoudnessDb = -12.0f;

        #region Сглаживание переключения

        private static int fadeMs = 15;
        private static int fadeSamples = (int) (Consts.SampleRate * (fadeMs / 1000f));
        private int fadePosition = 0;

        #endregion Сглаживание переключения

        #region Переключение эффектов

        private delegate void EffectDelegate(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right);

        private Dictionary<string, EffectDelegate> effects = new();
        private EffectDelegate? currentEffect;
        private EffectDelegate? nextEffect;

        #endregion Переключение эффектов

        #endregion Поля

        #region Вспомогательные функции

        private void PrepareCrossfade(EffectDelegate effect, ReadOnlySpan<float> input, in Span<float> left, in Span<float> right,
            out Span<float> outLeft)
        {
            outLeft = new float[left.Length];
            Span<float> outRight = new float[right.Length];

            left.CopyTo(outLeft);
            right.CopyTo(outRight);

            effect(input, outLeft, outRight);
        }

        private void Crossfade(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right)
        {
            if (currentEffect == null || nextEffect == null)
                return;

            PrepareCrossfade(currentEffect, input, left, right, out Span<float> curLeft);
            PrepareCrossfade(nextEffect, input, left, right, out Span<float> nextLeft);

            for (int i = 0; i < curLeft.Length; i++)
            {
                if (fadePosition < fadeSamples)
                {
                    // Прогресс перехода от 0.0 (начало) до 1.0 (конец)
                    float t = (float)fadePosition / fadeSamples;

                    // Вариант Б: Равномощный кроссфейд (без провала громкости в середине)
                    float angle = t * MathF.PI * 0.5f;
                    left[i] = curLeft[i] * MathF.Cos(angle) + nextLeft[i] * MathF.Sin(angle);

                    fadePosition++;
                }
                else
                {
                    // Кроссфейд завершён
                    left[i] = nextLeft[i];
                }
            }

            left.CopyTo(right);

            if (fadePosition >= fadeSamples)
            {
                fadePosition = 0;

                Volatile.Write(ref currentEffect, nextEffect);
                Volatile.Write(ref nextEffect, null);
            }
        }

        /// <summary>
        /// Возвращает линейный множитель выходного гейна для нормализации громкости модели.
        /// </summary>
        public static float CalculateOutputGainLinear(NamModel model, float targetLoudnessDb = DefaultTargetLoudnessDb, float userTrimDb = 0.0f)
        {
            float compensationDb = 0.0f;

            if (model.Metadata.Loudness is { } modelLoudness)
            {
                // Вычисляем разницу между целевой громкостью и громкостью модели
                compensationDb = targetLoudnessDb - (float)modelLoudness;

                // Защита от экстремальных значений в битых файлах
                compensationDb = Math.Clamp(compensationDb, -36.0f, 36.0f);
            }

            float totalDb = compensationDb + userTrimDb;
            return MathF.Pow(10f, totalDb / 20f);
        }

        #endregion Вспомогательные функции

        #region Основные функции

        public void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right)
        {
            if (nextEffect != null)
                if (currentEffect == null)
                    currentEffect = nextEffect;
                else
                {
                    // Перекрытие при переключении эффектов
                    Crossfade(input, left, right);
                    return;
                }

            if (currentEffect == null)
                return;

            currentEffect(input, left, right);
        }

        public void Init(Dictionary<string, NAMDescriptor> options)
        {
            effects = options
                .Where(o => File.Exists(o.Value.ModelPath))
                .ToDictionary(o => o.Key, o => {
                    Span<float> empty = new float[1024];

                    NAMDescriptor desc = o.Value;
                    NamModel model = NamModel.Load(desc.ModelPath);
                    NamProcessor processor = model.CreateProcessor();
                    processor.Process(empty, empty);
                    
                    float outputGain = CalculateOutputGainLinear(model) * desc.OutputGain;

                    // Есть файл импульса и модель без кабинета
                    CabinetEffect cabinet = null;
                    if (File.Exists(desc.CabinetImpulse))
                    {
                        cabinet = new();
                        cabinet.LoadImpulseResponse(desc.CabinetImpulse);
                        cabinet.Configure(WaveFormat.CreateIeeeFloatWaveFormat(Consts.SampleRate, 1));
                        // Прогрев, чтобы не было щёлчков
                        cabinet.Process(empty);
                    }

                    return new EffectDelegate((input, in left, in right) => {
                        processor.Process(left, left);
                        cabinet?.Process(left);

                        // Корректируем выходную громкость
                        for (int i = 0; i < left.Length; i++)
                            left[i] *= outputGain;

                        left.CopyTo(right);
                    });
                });
        }

        public void SetEffect(string name)
        {
            effects.TryGetValue(name, out nextEffect);
        }

        #endregion Основные функции

        #region IDisposable

        public void Dispose()
        {

        }

        #endregion IDisposable
    }
}
