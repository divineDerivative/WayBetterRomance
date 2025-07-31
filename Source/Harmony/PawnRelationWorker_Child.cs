using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Do not force a spouse relation on a newly generated parent if settings do not allow spouses
    //Actually need to have it run for parents with orientation mismatch
    [HarmonyPatch(typeof(PawnRelationWorker_Child), nameof(PawnRelationWorker_Child.CreateRelation))]
    public static class PawnRelationWorker_Child_CreateRelation
    {
        //generated is the parent, other is the child
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            LocalBuilder otherParent = ilg.DeclareLocal(typeof(Pawn));
            Label endLabel = ilg.DefineLabel();

            List<CodeInstruction> codes = instructions.SkipGayCheckTranspiler().GetAppropriateParentRelationshipTranspiler(new(OpCodes.Ldarg_1), new(OpCodes.Ldloc, otherParent.LocalIndex)).ToList();
            bool retAdded = false;
            foreach (CodeInstruction code in codes)
            {
                //Add my label to the end
                if (!retAdded && code.opcode == OpCodes.Ret)
                {
                    code.labels.Add(endLabel);
                    retAdded = true;
                }
                //This checks if spouse was added before trying to resolve the name
                if (code.Calls(typeof(SpouseRelationUtility), nameof(SpouseRelationUtility.ResolveNameForSpouseOnGeneration)))
                {
                    //if (generated.relations.DirectRelationExists(PawnRelationDefOf.Spouse, otherParent))
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.relations));
                    yield return InfoHelper.DefOfSpouse.LoadField();
                    yield return new(OpCodes.Ldloc, otherParent.LocalIndex);
                    yield return CodeInstruction.Call(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.DirectRelationExists));
                    //request is left on the stack, so get rid of it
                    yield return new(OpCodes.Pop);
                    yield return new(OpCodes.Brfalse_S, endLabel);
                    yield return new(OpCodes.Ldarg_3);
                    yield return new(OpCodes.Ldarg_1);
                }

                yield return code;

                //This is just to save info to a local variable
                if (code.Calls(AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.SetFather))))
                {
                    //otherParent = other.GetMother();
                    yield return new(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetMother));
                    yield return new(OpCodes.Stloc, otherParent.LocalIndex);
                }
                if (code.Calls(AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.SetMother))))
                {
                    //otherParent = other.GetFather();
                    yield return new(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetFather));
                    yield return new(OpCodes.Stloc, otherParent.LocalIndex);
                }
            }
        }
    }
}
