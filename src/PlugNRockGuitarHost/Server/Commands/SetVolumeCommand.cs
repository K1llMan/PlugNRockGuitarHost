namespace PlugNRockGuitarHost.Server.Commands
{
    public class SetVolumeCommand : BasicCommand<float?, object?>
    {
        public override string Type => "setVolume";

        public SetVolumeCommand(Func<float?, object?> action) : base(action)
        {
        }
    }
}