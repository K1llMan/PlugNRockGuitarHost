namespace PlugNRockGuitarHost.Processor.Effects
{
    public interface IProcessorEffect: IDisposable
    {
        bool Enabled { get; }
        void SetEnabled(bool enabled);
        void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right);
    }
}