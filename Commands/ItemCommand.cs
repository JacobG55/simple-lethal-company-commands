using GameNetcodeStuff;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

namespace SimpleCommands.Commands
{
    public class ItemCommand : SimpleCommand
    {
        public ItemCommand() : base("item", "spawns item")
        {
            instructions.Add("[/cmd] [item]");
            instructions.Add("[/cmd] [target] [item]");
            instructions.Add("[/cmd] [item] [x] [y] [z]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = false;

            if (sender.IsHost || sender.IsServer)
            {
                string itemName = "";
                Vector3 spawnPos = sender.transform.position;

                if (!parameters.IsEmpty())
                {
                    string first = parameters.GetString();

                    if (parameters.Count() >= 4)
                    {
                        if (parameters.GetRelativeVector(sender.transform.position, out Vector3 pos))
                        {
                            itemName = first;
                            spawnPos = pos;
                        }
                        else return UnknownVectorException();
                    }
                    else if (parameters.Count(2))
                    {
                        PlayerControllerB? player = GetPlayer(first);
                        itemName = parameters.GetString();

                        if (player != null)
                        {
                            spawnPos = player.transform.position;
                        }
                        else
                        {
                            return UnknownPlayerException(name);
                        }
                    }
                    else
                    {
                        itemName = first;
                    }
                }

                Terminal terminal = GetTerminal();
                if (terminal == null)
                {
                    return MissingTerminal();
                }

                List<Item> foundMatches = new List<Item>();
                foreach (Item item in RoundManager.Instance.playersManager.allItemsList.itemsList)
                {
                    if (item.itemName.ToLower().Replace(' ', '_').StartsWith(itemName.ToLower()))
                    {
                        foundMatches.Add(item);
                    }
                }

                if (foundMatches.Count > 0)
                {
                    int smallest = 0;
                    for (int i = 0; i < foundMatches.Count; i++)
                    {
                        if (foundMatches[i].itemName.Length < foundMatches[smallest].itemName.Length)
                        {
                            smallest = i;
                        }
                    }

                    GameObject obj = GrabbableObject.Instantiate(foundMatches[smallest].spawnPrefab, spawnPos, Quaternion.identity);
                    GrabbableObject spawned = obj.GetComponent<GrabbableObject>();
                    spawned.fallTime = 0f;
                    if (foundMatches[smallest].isScrap)
                    {
                        spawned.SetScrapValue(Random.Range(foundMatches[smallest].minValue, foundMatches[smallest].maxValue));
                    }
                    spawned.GetComponent<NetworkObject>().Spawn();

                    success = true;
                    return $"Spawned {foundMatches[smallest].itemName} at {spawnPos}.";
                }

                return "Unknown Item: " + itemName;
            }
            return "";
        }
    }

    public class ItemsCommand : SimpleCommand
    {
        public ItemsCommand() : base("items", "lists items") 
        { 
            overrideShowOutput = true;
            permissionRequired = false;

            instructions.Add("[/cmd] - lists item ids");
            instructions.Add("[/cmd] [page]");

            tagInfo.Add("'Store':\nFilters to show store items");
            tagInfo.Add("'Scrap':\nFilters to show scrap items");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            List<Item> items = new List<Item>();
            string title = "";

            bool storeItems = parameters.isFlagged("store");
            bool scrapItems = parameters.isFlagged("scrap");

            if (storeItems || scrapItems)
            {
                if (storeItems)
                {
                    title = "Buyable Items:";
                    Terminal terminal = GetTerminal();
                    if (terminal == null)
                    {
                        success = false;
                        return MissingTerminal();
                    }

                    items.AddRange(terminal.buyableItemsList);
                }
                if (scrapItems)
                {
                    title = "Scrap for Moon:";
                    foreach (SpawnableItemWithRarity spawnable in StartOfRound.Instance.currentLevel.spawnableScrap) 
                    {
                        items.Add(spawnable.spawnableItem);
                    }
                }
                if (storeItems && scrapItems)
                {
                    title = "Buyables & Scrap:";
                }
            }
            else
            {
                title = "All Items:";
                items.AddRange(RoundManager.Instance.playersManager.allItemsList.itemsList);
            }
            List<string> names = new List<string>();

            foreach (Item item in items)
            {
                names.Add(item.itemName.Replace(' ', '_'));
            }

            int page = 0;
            if (!parameters.IsEmpty())
            {
                page = parameters.GetNumber();
            }

            success = true;
            ClearChat();
            return PagedList(title, names, page, 8);
        }
    }
}
