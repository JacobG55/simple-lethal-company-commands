using GameNetcodeStuff;
using System;
using System.Collections.Generic;

namespace SimpleCommands.Commands
{
    public class ListCommand : SimpleCommand
    {
        public ListCommand() : base("list", "lists players") 
        { 
            overrideShowOutput = true;
            permissionRequired = false;

            instructions.Add("[/cmd]");
            instructions.Add("[/cmd] [page]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            List<string> usernames = new List<string>();
            PlayerControllerB[] players = RoundManager.Instance.playersManager.allPlayerScripts;

            foreach (PlayerControllerB player in players)
            {
                if (player.isPlayerControlled && !player.isPlayerDead)
                {
                    usernames.Add(player.playerUsername.Replace(' ', '_'));
                }
            }

            int page = 0;
            if (!parameters.IsEmpty())
            {
                page = parameters.GetNumber();
            }

            success = true;
            return PagedList("Players in Lobby:", usernames, page, 4);
        }
    }

    public class HelpCommand : SimpleCommand
    {
        public HelpCommand() : base("help", "lists commands") 
        { 
            overrideShowOutput = true;
            permissionRequired = false;

            instructions.Add("[/cmd] - Lists Commands");
            instructions.Add("[/cmd] [page]");
            instructions.Add("[/cmd] [cmd]");
            instructions.Add("[/cmd] [cmd] [page]");
            instructions.Add("[/cmd] [cmd] (flags)");
            instructions.Add("[/cmd] [cmd] (flags) [page]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = true;
            int page = 0;

            if (!parameters.IsEmpty())
            {
                page = parameters.GetNumberAt(0, out bool isFirstNumber);

                if (!isFirstNumber)
                {
                    string commandName = parameters.GetString();
                    SimpleCommand? command = tryGetCommand(commandName);

                    if (command != null)
                    {
                        if (parameters.Count(2))
                        {
                            string second = parameters.GetString();

                            if (second == "flags")
                            {
                                if (parameters.Count(3))
                                {
                                    page = parameters.GetNumberAt(2);
                                }
                                ClearChat();
                                return PagedList($"{command.name.ToUpper()} Accepted Flags:", command.tagInfo, page, 4);
                            }

                            page = parameters.GetNumberAt(1);
                        }
                        ClearChat();
                        return PagedList($"{command.name.ToUpper()} Use Cases:\n{GetPrefix()}help {command.name} flags\nfor flag info]", command.instructions, page).Replace("[/cmd]", GetPrefix() + commandName);
                    }
                    else
                    {
                        if (commandName.ToLower() == "flags")
                        {
                            return $"Flags Example:\n{GetPrefix()}pay | hide\n{GetPrefix()}help [cmd] flags\nto see flag modifiers.";
                        }
                    }
                }
            }
            List<string> entries =  new List<string>();
            foreach (SimpleCommand command in GetCommands())
            {
                entries.Add($"{GetPrefix()}{command.name} - {command.description}");
            }
            ClearChat();
            return PagedList("List of Commands:", entries, page);
        }
    }
}
