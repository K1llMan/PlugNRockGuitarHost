namespace PlugNRockGuitarHost.Processor.Effects.NoteDetector
{
    public class NotesChangedEventArgs : EventArgs
    {
        public List<int> MidiNotes { get; }
        public DateTime Time { get; } = DateTime.Now;

        public NotesChangedEventArgs(List<int> midiNotes)
        {
            MidiNotes = midiNotes;
        }
    }

    public interface INoteDetector : IProcessorEffect
    {
        static string GetNoteName(int midiNote)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int noteIndex = (midiNote % 12 + 12) % 12;
            int octave = (midiNote / 12) - 1;

            return $"{noteNames[noteIndex]}{octave}";
        }

        List<int> MidiNotes { get; }
        List<string> Notes { get; }
        event EventHandler<NotesChangedEventArgs> NotesChanged;
    }
}