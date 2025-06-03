using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleCommands.Commands
{
    public class RouteCommand : SimpleCommand
    {
        public RouteCommand() : base("route", "route the ship to a moon for free")
        {
            instructions.Add("[/cmd] [numberless_planetname]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = false;

            if (!StartOfRound.Instance.CanChangeLevels()) return "Can not route ship at this time.";
            if (parameters.IsEmpty()) return "No Level Name Provided.";

            string numberlessName = parameters.GetString();
            List<KeyValuePair<string, SelectableLevel>> levels = new List<KeyValuePair<string, SelectableLevel>>();
            int smallest = 0;

            foreach (SelectableLevel lvl in StartOfRound.Instance.levels)
            {
                string name = new string(lvl.PlanetName.SkipWhile((char c) => !char.IsLetter(c)).ToArray());
                if (name.StartsWith(numberlessName, StringComparison.InvariantCultureIgnoreCase))
                {
                    levels.Add(new KeyValuePair<string, SelectableLevel>(name, lvl));
                    if (name.Length < levels[smallest].Key.Length) smallest = levels.Count - 1;
                }
            }

            if (levels.Count > 0)
            {
                if (StartOfRound.Instance.IsServer)
                {
                    StartOfRound.Instance.ChangeLevelClientRpc(levels[smallest].Value.levelID, GetTerminal().groupCredits);
                }

                success = true;
                return $"Routing to {levels[smallest].Value.PlanetName}";
            }

            return $"Unknown level: {numberlessName}";
        }
    }
}
