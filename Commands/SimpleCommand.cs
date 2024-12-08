using GameNetcodeStuff;
using JLL.API;
using JLL.API.LevelProperties;
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

        public SimpleCommand(string name, string description)
        {
            this.name = name;
            this.description = description;
        }

        public abstract string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success);

        public string PagedList(string header, List<string> entries, int page, int pageMax = 5)
        {
            List<string> pageContent = new List<string>();
            int max = (int)Math.Ceiling(entries.Count / (float)pageMax);
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

        public static string UnknownPlayerException(string username) => $"{(username == "" ? "Unknown" : $"\"{username}\"")} is not in this lobby.";
        public static string UnknownNumberException() => "A Number was not able to be parsed.";
        public static string UnknownVectorException() => "A Position (X Y Z) was not able to be parsed.";
        public static string MissingTerminal() => "Missing Terminal.";

        private static readonly List<SimpleCommand> SimpleCommands = new List<SimpleCommand>();
        public static List<SimpleCommand> GetCommands() => SimpleCommands;
        public static string GetPrefix() => SimpleCommandsBase.commandPrefix.Value;

        public static void Register(SimpleCommand command)
        {
            if (!command.overrideShowOutput)
            {
                command.tagInfo.Add("'Hide':\nHides command chat output.");
            }

            SimpleCommands.Add(command);
            SimpleCommandsBase.LogInfo($"Registered Simple Command: {GetPrefix()} {command.name}", JLogLevel.Debuging);
        }

        public static bool tryParseCommand(string cmd, out SimpleCommand? command, out CommandParameters? parameters, Vector3 targetPos)
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

            parameters = new CommandParameters(parameterValues.ToArray(), flagValues.ToArray(), targetPos);
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

        public static Terminal GetTerminal() => JLevelPropertyRegistry.GetTerminal();

        public static void ClearChat()
        {
            HUDManager hud = HUDManager.Instance;
            hud.ChatMessageHistory.Clear();
            hud.chatText.SetText("");
            hud.lastChatMessage = "";
        }

        public class CommandParameters
        {
            private string[] parameters;
            private string[] flags;
            private Vector3 targetPos;
            private int place = 0;

            public CommandParameters(string[] parameters, string[] flags, Vector3 targetPos)
            {
                this.parameters = parameters;
                this.flags = flags;
                this.targetPos = targetPos + Vector3.up;
            }

            public Vector3 GetTargetPos()
            {
                return targetPos;
            }

            public bool isFlagged(string flag)
            {
                for (int i = 0; i < flags.Length; i++)
                {
                    if (flags[i] == flag.ToLower())
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool Count(int value) => parameters.Length >= value;
            public int Count() => parameters.Length;
            public bool IsEmpty() => parameters.Length == 0;
            public string asString() => $"parameters: [{string.Join(", ", parameters)}], flags: [{string.Join(", ", flags)}]";
            public string GetLowerCase() => GetString().ToLower();

            public string GetString()
            {
                string value = GetStringAt(Math.Min(parameters.Length - 1, place));
                place++;
                return value;
            }

            public string GetStringAt(int index)
            {
                return parameters[index];
            }

            public string GetLast()
            {
                return GetStringAt(parameters.Length - 1);
            }

            public int GetNumber()
            {
                int value = GetNumberAt(Math.Min(parameters.Length - 1, place), out bool isNumber);
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
                PlayerControllerB? value = GetPlayerAt(Math.Min(parameters.Length - 1, place));
                place++;
                return value;
            }

            public PlayerControllerB? GetPlayerAt(int index) => SimpleCommand.GetPlayer(parameters[index]);
            public bool GetVector(out Vector3 vector) => GetRelativeVector(Vector3.zero, out vector);
            public bool GetVectorAt(int startIndex, out Vector3 vector) => GetRelativeVectorAt(startIndex, Vector3.zero, out vector);
            public bool GetRelativeVector(Vector3 origin, out Vector3 vector)
            {
                bool value = GetRelativeVectorAt(Math.Min(parameters.Length - 1, place), origin, out vector);
                place += 3;
                return value;
            }

            public bool GetRelativeVectorAt(int startIndex, Vector3 origin, out Vector3 vector)
            {
                vector = Vector3.zero;
                if (startIndex + 2 < parameters.Length)
                {
                    float[] posNum = new float[3] { 0, 0, 0 };

                    for (int i = 0; i < 3; i++)
                    {
                        string pos = GetStringAt(startIndex + i);

                        if (float.TryParse(pos, out float parsed))
                        {
                            posNum[i] = parsed;
                        }
                        else if (GetRelative(pos, i, origin, out float output))
                        {
                            posNum[i] = output;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    vector = new Vector3(posNum[0], posNum[1], posNum[2]);
                    return true;
                }
                return false;
            }

            private bool GetRelative(string num, int i, Vector3 origin, out float output)
            {
                output = 0;
                switch (num[0])
                {
                    case '~':
                        output = i switch { 0 => origin.x, 1 => origin.y, 2 => origin.z, _ => origin.magnitude };
                        break;
                    case '^':
                        output = i switch { 0 => targetPos.x, 1 => targetPos.y, 2 => targetPos.z, _ => targetPos.magnitude };
                        break;
                    default:
                        return false;
                }
                if (float.TryParse(num[1..], out float parsed))
                {
                    output += parsed;
                }
                return true;
            }
        }
    }
}
