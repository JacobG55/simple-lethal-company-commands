using GameNetcodeStuff;
using JLL.API;
using JLL.API.LevelProperties;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public string PagedList(string header, List<string> entries, int page, int pageMax = 6)
        {
            if (entries.Count == 0) return $"{header}\nNo Entries Found.";
            List<string> pageContent = [];
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

        public static void Register(string name, Action<CommandParameters> action, string description = "")
            => Register(new ActionCommand(name, description, action));

        public static void Register(SimpleCommand command)
        {
            if (!command.overrideShowOutput)
            {
                command.tagInfo.Add("'Hide':\nHides command chat output.");
            }

            SimpleCommands.Add(command);
            command.OnRegister();
            SimpleCommandsBase.LogInfo($"Registered Simple Command: {GetPrefix()} {command.name}", JLogLevel.Debuging);
        }

        public virtual void OnRegister() { }

        public static bool tryParseCommand(string cmd, out SimpleCommand? command, out CommandParameters? parameters, Vector3 targetPos)
        {
            parameters = null;

            string[] split = cmd.Split(' ');

            command = tryGetCommand(split[0]);

            if (command == null) return false;

            List<string> parameterValues = [];
            List<string> flagValues = [];

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
            public PlayerControllerB sender { internal set; get; }
            private readonly string[] parameters;
            private readonly string[] flags;
            private Vector3 targetPos;
            public int place = 0;

            private static readonly Dictionary<string, bool> boolValues = new Dictionary<string, bool>()
            {
                { "true", true },
                { "false", false },
                { "on", true },
                { "off", false },
                { "1", true },
                { "0", false },
            };

            public CommandParameters(string[] parameters, string[] flags, Vector3 targetPos)
            {
                this.parameters = parameters;
                this.flags = flags;
                this.targetPos = targetPos + Vector3.up;
            }

            public Vector3 GetTargetPos() => targetPos;

            public bool isFlagged(string flag)
            {
                for (int i = 0; i < flags.Length; i++)
                {
                    if (flags[i].Equals(flag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }

            public string[] GetFlags() => flags;

            public bool Count(int value) => parameters.Length >= value;
            public int Count() => parameters.Length;
            public bool IsEmpty() => parameters.Length == 0;
            public bool HasNext() => place < parameters.Length;
            public string asString() => $"parameters: [{string.Join(", ", parameters)}], flags: [{string.Join(", ", flags)}]";
            public string GetLowerCase() => GetString().ToLower();

            public string GetString()
            {
                place++;
                return GetStringAt(Math.Min(parameters.Length - 1, place - 1));
            }

            public string GetStringAt(int index) => parameters[index];

            public string GetLast() => GetStringAt(parameters.Length - 1);

            public int GetNumber()
            {
                place++;
                return GetNumberAt(Math.Min(parameters.Length - 1, place - 1), out bool isNumber);
            }

            public int GetNumberAt(int index) => GetNumberAt(index, out bool isNumber);

            public int GetNumberAt(int index, out bool isNumber)
            {
                isNumber = int.TryParse(parameters[index], out int value);
                if (isNumber)
                {
                    return value;
                }
                return 0;
            }

            public float GetFloat()
            {
                place++;
                return GetFloatAt(Math.Min(parameters.Length - 1, place - 1), out bool isNumber);
            }

            public float GetFloatAt(int index) => GetFloatAt(index, out bool isNumber);

            public float GetFloatAt(int index, out bool isNumber)
            {
                isNumber = float.TryParse(parameters[index], out float value);
                if (isNumber)
                {
                    return value;
                }
                return 0;
            }

            public bool GetBool()
            {
                GetBoolAt(place, out bool value);
                place++;
                return value;
            }
            public bool GetBoolAt(int index)
            {
                GetBoolAt(index, out bool value);
                return value;
            }
            public bool GetBoolAt(int index, out bool value) 
                => boolValues.TryGetValue(parameters[index].ToLower(), out value);

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
