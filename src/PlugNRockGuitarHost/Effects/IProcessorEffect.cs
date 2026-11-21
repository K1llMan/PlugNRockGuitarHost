namespace PlugNRockGuitarHost.Effects
{
    public interface IProcessorEffect: IDisposable
    {
        void Process(ReadOnlySpan<float> input, in Span<float> left, in Span<float> right);
    }
}