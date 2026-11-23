using PlugNRockGuitarHost.Processor.Models;

namespace PlugNRockGuitarHost.Server.Commands
{
    public class SetNAMEffectCommand : BasicCommand<string?, object?>
    {
        public override string Type => "setNAMEffect";

        public SetNAMEffectCommand(Func<string?, object?> action) : base(action)
        {
        }
    }
}