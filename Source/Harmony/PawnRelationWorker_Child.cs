using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            FieldInfo Gay = AccessTools.Field(typeof(TraitDefOf), nameof(TraitDefOf.Gay));
            FieldInfo DefOfSpouse = AccessTools.Field(typeof(PawnRelationDefOf), nameof(PawnRelationDefOf.Spouse));
            LocalBuilder otherParent = ilg.DeclareLocal(typeof(Pawn));
            Label endLabel = ilg.DefineLabel();

            List<CodeInstruction> codes = instructions.ToList();
            codes[codes.Count - 1].labels.Add(endLabel);

            bool gayFound = false;
            bool retAdded = false;
            foreach (CodeInstruction code in codes)
            {
                //This bit gets rid of the gay check by changing brfalse to br
                //Should do it twice, since there's two gay checks
                if (code.LoadsField(Gay))
                {
                    gayFound = true;
                }
                if (gayFound && code.Branches(out _))
                {
                    //Need to remove the bool from the stack first
                    yield return new CodeInstruction(OpCodes.Pop);
                    code.opcode = OpCodes.Br_S;
                    gayFound = false;
                }
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
                    yield return DefOfSpouse.LoadField();
                    yield return new CodeInstruction(OpCodes.Ldloc, otherParent.LocalIndex);
                    yield return CodeInstruction.Call(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.DirectRelationExists));
                    //request is left on the stack, so get rid of it
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Brfalse_S, endLabel);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                }
                //This is the only code we're actually replacing
                if (code.LoadsField(DefOfSpouse))
                {
                    //RomanceUtilities.GetAppropriateParentRelationship(generated, otherParent)
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc, otherParent.LocalIndex);
                    yield return CodeInstruction.Call(typeof(RomanceUtilities), nameof(RomanceUtilities.GetAppropriateParentRelationship));
                }
                else
                {
                    yield return code;
                }
                //This is just to save info to a local variable
                if (code.Calls(AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.SetFather))))
                {
                    //otherParent = other.GetMother();
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetMother));
                    yield return new CodeInstruction(OpCodes.Stloc, otherParent.LocalIndex);
                }
                if (code.Calls(AccessTools.Method(typeof(ParentRelationUtility), nameof(ParentRelationUtility.SetMother))))
                {
                    //otherParent = other.GetFather();
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetFather));
                    yield return new CodeInstruction(OpCodes.Stloc, otherParent.LocalIndex);
                }
            }
        }
    }
}