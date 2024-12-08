using JLL.API;
using LethalLib.Modules;
using SimpleCommands;
using static Simple_Commands.Commands.SimplePrefabs;

namespace Simple_Commands.Managers
{
    public static class LethalLibCompatibility
    {
        internal static void RegisterLethalLibPrefabs()
        {
            SimpleCommandsBase.LogInfo($"LethalLib Map Hazards: {MapObjects.mapObjects.Count}", JLogLevel.Debuging);

            foreach (var mapObject in MapObjects.mapObjects)
            {
                RegisterSimplePrefab(mapObject.mapObject.prefabToSpawn.name, new SimplePrefab { prefab = mapObject.mapObject.prefabToSpawn });
            }
        }
    }
}
