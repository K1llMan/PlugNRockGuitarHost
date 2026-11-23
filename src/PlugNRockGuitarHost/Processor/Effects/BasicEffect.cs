namespace PlugNRockGuitarHost.Processor.Effects
{
    public class BasicEffect : IProcessorEffect
    {
        #region Свойства

        public bool Enabled { get; private set; } = true;

        #endregion Свойства

        #region IProcessorEffect

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public virtual void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right)
        {
            throw new NotImplementedException();
        }

        #endregion IProcessorEffect

        #region IDisposable

        public virtual void Dispose()
        {
        }

        #endregion IDisposable
    }
}