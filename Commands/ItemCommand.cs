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
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = false;

            if (sender.IsHost || sender.IsServer)
            {
                PlayerControllerB effected = sender;
                string itemName = "";

                if (!parameters.IsEmpty())
                {
                    string first = parameters.GetString();

                    if (parameters.Count(2))
                    {
                        PlayerControllerB? player = GetPlayer(first);
                        itemName = parameters.GetString();

                        if (player != null)
                        {
                            effected = player;
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

                List<Item> spawnables = RoundManager.Instance.playersManager.allItemsList.itemsList;
                foreach (Item item in spawnables)
                {
                    if (item.itemName.ToLower().Replace(' ', '_') == itemName.ToLower())
                    {
                        GameObject obj = GrabbableObject.Instantiate(item.spawnPrefab, effected.transform.position, Quaternion.identity);
                        GrabbableObject spawned = obj.GetComponent<GrabbableObject>();
                        spawned.GetComponent<GrabbableObject>().fallTime = 0f;
                        spawned.GetComponent<NetworkObject>().Spawn();

                        success = true;
                        return "Gave " + effected.playerUsername + " " + item.itemName + ".";
                    }
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
