using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleCommands.Commands
{
    public abstract class SimpleCommand
    {
        public string name;
        public string description;
        public bool permissionRequired = true;
        public bool overrideShowOutput = false;
        public List<string> instructions = new List<string>();
        public List<string> tagInfo = new List<string>();

        private static Terminal Terminal;

        public SimpleCommand(string name, string description)
        {
            this.name = name;
            this.description = description;
        }

        public abstract string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success);

        public string PagedList(string header, List<string> entries, int page, int pageMax = 5)
        {
            List<string> pageContent = new List<string>();
            int max = (int)Math.Ceiling(entries.Count / (pageMax + 0f));
            page = Math.Clamp(page - 1, 0, max-1);

            if (entries.Count > pageMax)
            {
                for (int i = Math.Clamp(page * pageMax, 0, entries.Count - 1); i < Math.Min(entries.Count, (page + 1) * pageMax); i++)
                {
                    pageContent.Add(entries[i]);
                }
                pageContent.Add($"[Page {Math.Min(page + 1, max)} of {max}]");
            }
            else
            {
                pageContent = entries;
            }

            return $"{header}\n{string.Join("\n", pageContent)}";
        }

        public string UnknownPlayerException(string username)
        {
            return "\"" + username + "\" is not in this lobby.";
        }

        public string UnknownNumberException()
        {
            return "A Number was not able to be parsed.";
        }

        public string UnknownVectorException()
        {
            return "A Position (X Y Z) was not able to be parsed.";
        }

        public string MissingTerminal()
        {
            return "Missing Terminal.";
        }

        private static readonly List<SimpleCommand> SimpleCommands = new List<SimpleCommand>();
        public static List<SimpleCommand> GetCommands()
        {
            return SimpleCommands;
        }

        public static string GetPrefix()
        {
            return SimpleCommandsBase.commandPrefix.Value;
        }

        public static void Register(SimpleCommand command)
        {
            if (!command.overrideShowOutput)
            {
                command.tagInfo.Add("'Hide':\nHides command chat output.");
            }

            SimpleCommands.Add(command);
            SimpleCommandsBase.Instance.mls.LogInfo($"Registered Simple Command: {GetPrefix()} {command.name}");
        }

        public static bool tryParseCommand(string cmd, out SimpleCommand? command, out CommandParameters? parameters)
        {
            parameters = null;

            string[] split = cmd.Split(' ');

            command = tryGetCommand(split[0]);

            if (command == null) return false;

            List<string> parameterValues = new List<string>();
            List<string> flagValues = new List<string>();

            bool parameter = true;
            for (int i = 1; i < split.Length; i++)
            {
                if (parameter)
                {
                    if (split[i] == "")
                    {
                        continue;
                    }
                    if (split[i] == "|")
                    {
                        parameter = false; continue;
                    }
                    parameterValues.Add(split[i]);
                }
                else
                {
                    flagValues.Add(split[i].ToLower());
                }
            }

            parameters = new CommandParameters(parameterValues, flagValues);
            return true;
        }

        public static SimpleCommand? tryGetCommand(string name)
        {
            foreach (SimpleCommand simpleCommand in SimpleCommands)
            {
                if (simpleCommand.name.ToLower() == name.ToLower())
                {
                    return simpleCommand;
                }
            }
            return null;
        }

        public static PlayerControllerB? GetPlayer(string name)
        {
            List<PlayerControllerB> playersMatches = new List<PlayerControllerB>();
            foreach (PlayerControllerB player in RoundManager.Instance.playersManager.allPlayerScripts)
            {
                if (player.playerUsername.ToLower().Replace(' ', '_').StartsWith(name.ToLower().Replace(' ', '_')))
                {
                    playersMatches.Add(player);
                }
            }
            if (playersMatches.Count > 0)
            {
                int smallest = 0;
                for (int i = 0; i < playersMatches.Count; i++)
                {
                    if (playersMatches[i].name.Length < playersMatches[smallest].name.Length)
                    {
                        smallest = i;
                    }
                }
                return playersMatches[smallest];
            }
            return null;
        }

        public static Terminal GetTerminal()
        {
            if (Terminal)
            {
                return Terminal;
            }
            else
            {
                return Terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
            }
        }

        public static void ClearChat()
        {
            HUDManager hud = HUDManager.Instance;
            hud.ChatMessageHistory.Clear();
            hud.chatText.SetText("");
            hud.lastChatMessage = "";
        }

        public class CommandParameters
        {
            private List<string> parameters;
            private List<string> flags;
            private int place = 0;

            public CommandParameters(List<string> parameters, List<string> flags)
            {
                this.parameters = parameters;
                this.flags = flags;
            }

            public bool isFlagged(string flag)
            {
                return flags.Contains(flag.ToLower());
            }

            public bool Count(int value)
            {
                return parameters.Count >= value;
            }

            public int Count()
            {
                return parameters.Count;
            }

            public bool IsEmpty()
            {
                return parameters.Count == 0;
            }

            public string asString()
            {
                return "parameters: [" + string.Join(", ", parameters.ToArray()) + "], flags: [" + string.Join(", ", flags.ToArray()) + "]";
            }

            public string GetLowerCase()
            {
                return GetString().ToLower();
            }

            public string GetString()
            {
                string value = GetStringAt(Math.Min(parameters.Count-1, place));
                place++;
                return value;
            }

            public string GetStringAt(int index)
            {
                return parameters[index];
            }

            public string GetLast()
            {
                return GetStringAt(parameters.Count-1);
            }

            public int GetNumber()
            {
                int value = GetNumberAt(Math.Min(parameters.Count-1, place), out bool isNumber);
                place++;
                return value;
            }

            public int GetNumberAt(int index)
            {
                return GetNumberAt(index, out bool isNumber);
            }

            public int GetNumberAt(int index, out bool isNumber)
            {
                isNumber = int.TryParse(parameters[index], out int value);
                if (isNumber)
                {
                    return value;
                }
                return 0;
            }

            public PlayerControllerB? GetPlayer()
            {
                PlayerControllerB? value = GetPlayerAt(Math.Min(parameters.Count - 1, place));
                place++;
                return value;
            }

            public PlayerControllerB? GetPlayerAt(int index)
            {
                return SimpleCommand.GetPlayer(parameters[index]);
            }

            public bool GetVector(out Vector3 vector)
            {
                return GetRelativeVector(Vector3.zero, out vector);
            }

            public bool GetVectorAt(int startIndex, out Vector3 vector)
            {
                return GetRelativeVectorAt(startIndex, Vector3.zero, out vector);
            }

            public bool GetRelativeVector(Vector3 origin, out Vector3 vector)
            {
                bool value = GetRelativeVectorAt(Math.Min(parameters.Count - 1, place), origin, out vector);
                place += 3;
                return value;
            }

            public bool GetRelativeVectorAt(int startIndex, Vector3 origin, out Vector3 vector)
            {
                vector = Vector3.zero;
                if (startIndex + 2 < parameters.Count)
                {
                    int[] posNum = new int[3];
                    bool[] posRelative = new bool[3];

                    for (int i = 0; i < 3; i++)
                    {
                        string pos = GetStringAt(startIndex + i);

                        if (pos.StartsWith("~"))
                        {
                            posRelative[i] = true;
                            pos = pos.Substring(1);
                            if (!int.TryParse(pos, out posNum[i]))
                            {
                                posNum[i] = 0;
                            }
                        }
                        else
                        {
                            posRelative[i] = false;
                            if (!int.TryParse(pos, out posNum[i]))
                            {
                                return false;
                            }
                        }
                    }

                    vector = new Vector3(posNum[0] + (posRelative[0] ? origin.x : 0), posNum[1] + (posRelative[1] ? origin.y : 0), posNum[2] + (posRelative[2] ? origin.z : 0));
                    return true;
                }

                return false;
            }
        }
    }
}
