// <copyright file="NetDeserializePatch.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using AlgernonCommons;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to change curb hights on net load (before initial render generation to avoid needing to regenerate it later).
    /// </summary>
    [HarmonyPatch(typeof(NetManager.Data), nameof(NetManager.Data.AfterDeserialize))]
    public static class NetDeserializePatch
    {
        /// <summary>
        /// Harmomy transpiler to insert call to RaiseCurbHeights in NetManager.Data.AfterDeserialize.
        /// </summary>
        /// <param name="instructions">Original ILCode</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Going to find first (and only) stloc.s 7 in game code.  Immediately after that we insert a call to RaiseCurbHeights.  Simple.
            // This means that basic init is done by method but we raise the curb heights before the method starts looping through networks.

            // Instruction parsing.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;

            Logging.Message("starting NetManager.Data.AfterDeserialize transpiler");

            // Status flag.
            bool foundTarget = false;

            // Iterate through all instructions in original method.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction and add it to output.
                instruction = instructionsEnumerator.Current;

                // Are we still searching?
                if (!foundTarget)
                {
                    // Still searching for target - is this stloc.s 7?
                    if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == 7)
                    {
                        // Yes - add it to output.
                        yield return instruction;

                        // Insert call to our custom method immediately afterwards.
                        instruction = new CodeInstruction(OpCodes.Call, typeof(NetHandler).GetMethod(nameof(NetHandler.OnLoad)));

                        // Set flag.
                        foundTarget = true;
                    }
                }

                // Output instruction.
                yield return instruction;
            }

            // If we got here without finding our target, something went wrong.
            if (!foundTarget)
            {
                Logging.Error("no stloc.s 7 found");
            }
        }
    }
}