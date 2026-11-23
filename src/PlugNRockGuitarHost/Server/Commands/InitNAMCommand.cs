using PlugNRockGuitarHost.Processor.Models;

namespace PlugNRockGuitarHost.Server.Commands
{
    public class InitNAMCommand : BasicCommand<Dictionary<string, NAMDescriptor>?, object?>
    {
        public override string Type => "initNAM";

        public InitNAMCommand(Func<Dictionary<string, NAMDescriptor>?, object?> action) : base(action)
        {
        }
    }
}