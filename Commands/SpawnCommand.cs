using BepInEx;
using GameNetcodeStuff;
using JLL.API;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleCommands.Commands
{
    public class SpawnCommand<T> : SimpleCommand
    {
        private readonly Func<T, string> Identify;
        private readonly Func<string[], IEnumerable<T>> RetreiveCollection;

        private readonly bool RequiresNavmesh;
        private readonly Action<T, Vector3, CommandParameters> Result;

        public readonly ListCommand? ListCmd;

        public SpawnCommand(string name, string description, Func<T, string> identify, Func<string[], IEnumerable<T>> collection, Action<T, Vector3, CommandParameters> result, bool requireNavmesh = false) : base(name, description)
        {
            instructions.Add("[/cmd] [id]");
            instructions.Add("[/cmd] [id] [count]");
            instructions.Add("[/cmd] [id] [target]");
            instructions.Add("[/cmd] [id] [target] [count]");
            instructions.Add("[/cmd] [id] [x] [y] [z]");
            instructions.Add("[/cmd] [id] [x] [y] [z] [count]");

            if (requireNavmesh) tagInfo.Add("'Ignore':\nIgnores nav mesh restrictions (May Throw Errors)");
            else tagInfo.Add("'Snap':\nSnaps to nav mesh");
            tagInfo.Add("'Uncapped':\nIgnores cap to spawn commands");

            Identify = identify;
            RetreiveCollection = collection;
            RequiresNavmesh = requireNavmesh;
            Result = result;
        }

        public SpawnCommand(string name, string description, string listCmd, string listHeader, (string, string)[] filterInfos, Func<T, string> identify, Func<string[], IEnumerable<T>> collection, Action<T, Vector3, CommandParameters> result, bool requireNavmesh = false) 
            : this(name, description, identify, collection, result, requireNavmesh)
        {
            ListCmd = new ListCommand(this, listCmd, listHeader, filterInfos);
        }

        public override void OnRegister()
        {
            if (ListCmd != null) Register(ListCmd);
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = false;

            if (sender.IsHost || sender.IsServer)
            {
                string identifier = "";
                Vector3 spawnPos = parameters.GetTargetPos();
                int count = 1;

                if (!parameters.IsEmpty())
                {
                    identifier = parameters.GetString();

                    if (parameters.Count() >= 4)
                    {
                        if (parameters.GetRelativeVector(sender.transform.position, out Vector3 pos))
                        {
                            spawnPos = pos;
                        }
                        else return UnknownVectorException();

                        if (parameters.HasNext()) count = Math.Max(count, parameters.GetNumber());
                    }
                    else if (parameters.Count(2))
                    {
                        int num = parameters.GetNumberAt(1, out bool isNumber);

                        string playerName = parameters.GetString();
                        PlayerControllerB? player = GetPlayer(playerName);

                        if (isNumber)
                        {
                            count = num;
                        }
                        else if (player != null)
                        {
                            spawnPos = player.transform.position;
                            if (parameters.HasNext()) count = Math.Max(count, parameters.GetNumber());
                        }
                        else
                        {
                            return UnknownPlayerException(playerName);
                        }

                        if (player != null)
                        {
                            spawnPos = player.transform.position;
                        }
                    }
                }

                if (identifier.IsNullOrWhiteSpace()) return "Empty Identifer";

                List<T> foundMatches = [];
                int smallest = 0;
                string? smallestID = null;

                foreach (T item in RetreiveCollection.Invoke(ListCmd == null ? [] : ListCmd.filters.Keys.Where(parameters.isFlagged).ToArray()))
                {
                    string id = Identify.Invoke(item);
                    if (id.Replace(' ', '_').StartsWith(identifier, StringComparison.InvariantCultureIgnoreCase))
                    {
                        foundMatches.Add(item);
                        if (smallestID == null || id.Length < smallestID.Length)
                        {
                            smallest = foundMatches.Count - 1;
                            smallestID = id;
                        }
                    }
                }

                if (foundMatches.Count > 0 && smallestID != null)
                {
                    bool snap = parameters.isFlagged("snap");
                    if (RequiresNavmesh || snap)
                    {
                        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5, NavMesh.AllAreas))
                        {
                            IterateResult(foundMatches[smallest], hit.position, parameters, count);
                            success = true;
                            return $"Spawned {smallestID} at {hit.position}.";
                        }
                        else if (!snap && !parameters.isFlagged("ignore"))
                        {
                            return "Failed to spawn. (Couldn't Find NavMesh.)";
                        }
                    }

                    IterateResult(foundMatches[smallest], spawnPos, parameters, count);

                    success = true;
                    return $"Spawned {smallestID} at {spawnPos}.";
                }
                return $"Unknown Identifier: {smallestID}";
            }
            return "";
        }

        private void IterateResult(T item, Vector3 pos, CommandParameters parameters, int count)
        {
            int cap = parameters.isFlagged("uncapped") ? int.MaxValue : SimpleCommandsBase.spawnCap.Value;
            for (int i = 0; i < count && i < cap; i++) Result.Invoke(item, pos, parameters);
        }

        public class ListCommand : SimpleCommand
        {
            private readonly string header;
            private readonly SpawnCommand<T> parentCommand;
            public readonly Dictionary<string, string> filters;
            public ListCommand(SpawnCommand<T> parentCommand, string cmd, string header, (string, string)[] filters) : base(cmd, $"lists spawnable {cmd}")
            {
                instructions.Add($"[/cmd] - Lists {header.ToLower()}");
                instructions.Add("[/cmd] [page]");
                this.parentCommand = parentCommand;
                this.header = header;
                this.filters = new Dictionary<string, string>();

                foreach (var pair in filters)
                {
                    this.filters.Add(pair.Item1, pair.Item2);
                    tagInfo.Add($"'{pair.Item1}':\n{pair.Item2}");
                }
            }

            public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
            {
                success = true;
                if (!sender.IsLocalPlayer()) return "";
                ClearChat();
                string[] activeFilters = filters.Keys.Where(parameters.isFlagged).ToArray();
                return PagedList($"{header}:{(filters.Keys.Count > 0 ? $"\nFilters: {string.Join(' ', filters.Keys)}" : string.Empty)}", 
                    parentCommand.RetreiveCollection.Invoke(activeFilters).Select(parentCommand.Identify.Invoke).ToList(), parameters.IsEmpty() ? 0 : parameters.GetNumber(), 8);
            }
        }
    }
}
