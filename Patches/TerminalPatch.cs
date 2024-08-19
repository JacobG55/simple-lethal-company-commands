using HarmonyLib;
using System;

namespace Simple_Commands.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Terminal), "CallFunctionInAccessibleTerminalObject")]
        public static void terminalObjectRequest(Terminal instance, string word) =>
            throw new NotImplementedException("It's a stub");
    }
}
