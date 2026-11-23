namespace PlugNRockGuitarHost.Server.Commands
{
    public class SetPitchCommand : BasicCommand<int?, object?>
    {
        public override string Type => "setPitch";

        public SetPitchCommand(Func<int?, object?> action) : base(action)
        {
        }
    }
}