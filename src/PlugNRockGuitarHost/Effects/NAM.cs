using NAudio.Effects;
using NAudio.Nam;
using NAudio.Wave;
using PlugNRockGuitarHost.Models;

namespace PlugNRockGuitarHost.Effects
{
    public class NAM : IProcessorEffect
    {
        #region Поля
        private static int fadeMs = 15;
        private static int fadeSamples = (int) (Consts.SampleRate * (fadeMs / 1000f));

        private int fadePosition = 0;

        private delegate void EffectDelegate(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right);

        private Dictionary<string, EffectDelegate> effects = new();
        private EffectDelegate? currentEffect;
        private EffectDelegate? nextEffect;

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
                    NAMDescriptor desc = o.Value;
                    NamModel model = NamModel.Load(desc.ModelPath);
                    NamProcessor processor = model.CreateProcessor(0.0f);

                    CabinetEffect cabinet = null;
                    // Есть файл импульса и модель без кабинета
                    if (File.Exists(desc.CabinetImpulse) && !model.Metadata.GearType.Contains("cab"))
                    {
                        cabinet = new();
                        cabinet.LoadImpulseResponse(desc.CabinetImpulse);
                        cabinet.Configure(WaveFormat.CreateIeeeFloatWaveFormat(Consts.SampleRate, 1));
                    }

                    return new EffectDelegate((input, in left, in right) => {
                        processor.Process(left, left);
                        cabinet?.Process(left);

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
