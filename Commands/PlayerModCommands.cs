using GameNetcodeStuff;
using SimpleCommands.Commands;
using SimpleCommands.Components;

namespace Simple_Commands.Commands
{
    public abstract class BasePlayerModCommand : SimpleCommand
    {
        public BasePlayerModCommand(string name, string description, string extraParams = "") : base(name, description)
        {
            instructions.Add($"[/cmd] {extraParams}");
            instructions.Add($"[/cmd] [target] {extraParams}");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            PlayerControllerB effected = sender;
            success = false;

            if (!parameters.IsEmpty())
            {
                string name = parameters.GetStringAt(0);
                PlayerControllerB? player = GetPlayer(name);

                if (player != null)
                {
                    effected = player;
                    parameters.place++;
                }
            }

            if (effected.TryGetComponent(out PlayerModification mod))
            {
                success = true;
                return ToggleCheat(mod, parameters);
            }
            return "";
        }
        public abstract string ToggleCheat(PlayerModification playerMod, CommandParameters parameters);
    }

    public class AllCheatsCommand : BasePlayerModCommand
    {
        public AllCheatsCommand() : base("cheats", "enable / disable cheats", "(On | Off)") { }
        public override string ToggleCheat(PlayerModification playerMod, CommandParameters parameters)
        {
            bool enable = (parameters.HasNext() && parameters.GetBoolAt(parameters.place, out bool value)) ? value : true;

            playerMod.invulnerable = playerMod.infinateSprint = playerMod.enableFlying = enable;
            return $"{(enable ? "Enabled" : "Disabled")} All Cheats for {playerMod.player.playerUsername}";
        }
    }

    public class InfiniteSprintCommand : BasePlayerModCommand
    {
        public InfiniteSprintCommand() : base("stamina", "toggle infinate sprint") { }
        public override string ToggleCheat(PlayerModification playerMod, CommandParameters parameters)
        {
            playerMod.infinateSprint = !playerMod.infinateSprint;
            return $"Infinite Stamina for {playerMod.player.playerUsername} set to {playerMod.infinateSprint}";
        }
    }

    public class InvulnerabilityCommand : BasePlayerModCommand
    {
        public InvulnerabilityCommand() : base("god", "toggle invulnerability") { }
        public override string ToggleCheat(PlayerModification playerMod, CommandParameters parameters)
        {
            playerMod.invulnerable = !playerMod.invulnerable;
            return $"Invulnerability for {playerMod.player.playerUsername} set to {playerMod.invulnerable}";
        }
    }

    public class FlyCommand : BasePlayerModCommand
    {
        public FlyCommand() : base("fly", "toggle flight") { }
        public override string ToggleCheat(PlayerModification playerMod, CommandParameters parameters)
        {
            playerMod.enableFlying = !playerMod.enableFlying;
            return $"Can Fly for {playerMod.player.playerUsername} set to {playerMod.enableFlying}";
        }
    }
}
