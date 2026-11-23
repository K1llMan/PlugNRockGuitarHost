namespace PlugNRockGuitarHost.Processor.Effects
{
    public class VolumeControl : BasicEffect
    {
        #region Свойства

        public float Volume { get; set; } = 1.0f;
        public float Balance { get; set; } = 0.0f;

        #endregion Свойства

        #region Основные функции

        public override void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right)
        {
            float leftGain = Volume * (Balance > 0f ? 1.0f - Balance : 1.0f);
            float rightGain = Volume * (Balance < 0f ? 1.0f + Balance : 1.0f);

            for (int i = 0; i < left.Length; i++)
            {
                left[i] *= leftGain;
                right[i] *= rightGain;
            }
        }


        #endregion Основные функции
    }
}