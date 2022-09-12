namespace CurbHeightAdjuster
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using AlgernonCommons;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to change parking lot road 'buildings' on building load (before initial render generation to avoid needing to regenerate it later).
    /// </summary>
    [HarmonyPatch(typeof(BuildingManager.Data), nameof(BuildingManager.Data.AfterDeserialize))]
    public static class BuildingDeserializePatch
    {
        /// <summary>
        /// Harmomy transpiler to insert call to RaiseCurbHeights in NetManager.Data.AfterDeserialize.
        /// </summary>
        /// <param name="instructions">Original ILCode</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Going to find first (and only) stloc.2 in game code.  Immediately after that we insert a call to RaiseParkingLots.  Simple.
            // This means that basic init is done by method but we raise the curb heights before the method starts looping through buildings and generating renders.

            // Instruction parsing.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;

            Logging.Message("starting BuildingManager.Data.AfterDeserialize transpiler");


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
                    // Still searching for target - is this stloc.2?
                    if (instruction.opcode == OpCodes.Stloc_2)
                    {
                        // Yes - add it to output.
                        yield return instruction;

                        // Insert call to our custom method immediately afterwards.
                        instruction = new CodeInstruction(OpCodes.Call, typeof(ParkingLots).GetMethod(nameof(ParkingLots.RaiseParkingLots)));

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
                Logging.Error("no stloc.2 found");
            }
        }
    }
}