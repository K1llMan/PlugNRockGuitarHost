namespace PlugNRockGuitarHost.Processor
{
    public class GuitarProcessorOptions
    {
        public string Driver { get; set; }
        public float Volume { get; set; } = 1.0f;
        public float Balance { get; set; } = 0.0f;
        public int Pitch { get; set; } = 0;
    }
}