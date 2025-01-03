using DivineFramework.UI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BetterRomance
{
    public partial class Settings : ModSettings
    {
        public OrientationHolder sexualOrientations = new()
        {
            standardOrientation = new()
            {
                hetero = 20f,
                homo = 20f,
                bi = 50f,
                none = 10f,
                enby = 10f,
            }
        };
        public OrientationHolder romanticOrientations = new()
        {
            standardOrientation = new()
            {
                hetero = 20f,
                homo = 20f,
                bi = 50f,
                none = 10f,
                enby = 10f,
            }
        };
        ////Settings for non-complex orientations
        //public OrientationChances sexualOrientations = new()
        //{
        //    hetero = 20f,
        //    homo = 20f,
        //    bi = 50f,
        //    none = 10f,
        //    enby = 10f,
        //};
        //public OrientationChances romanticOrientations = new()
        //{
        //    hetero = 20f,
        //    homo = 20f,
        //    bi = 50f,
        //    none = 10f,
        //    enby = 10f,
        //};

        ////Per gender chances, to be used for display only I think
        //public GenderAttractionChances sexualAttractionForMen = new();
        //public GenderAttractionChances sexualAttrationForWomen = new();
        //public GenderAttractionChances sexualAttractionForEnby = new();
        //public GenderAttractionChances romanticAttractionForMen = new();
        //public GenderAttractionChances romanticAttrationForWomen = new();
        //public GenderAttractionChances romanticAttractionForEnby = new();

        ////Orientation equivalents of the above, to be used in the code
        //public OrientationChances sexualOrientationForMen = new();
        //public OrientationChances sexualOrientationForWomen = new();
        //public OrientationChances sexualOrientationForEnby = new();
        //public OrientationChances romanticOrientationForMen = new();
        //public OrientationChances romanticOrientationForWomen = new();
        //public OrientationChances romanticOrientationForEnby = new();

        public float dateRate = 100f;
        public float hookupRate = 100f;
        public float cheatChance = 100f;
        public float alienLoveChance = 33f;
        public int minOpinionRomance = 5;
        public int minOpinionHookup = 0;
        public IntRange cheatingOpinion = new(-75, 75);

        public static string fertilityMod = "None";
        public bool joyOnSlaves = false;
        public bool joyOnPrisoners = false;
        public bool joyOnGuests = false;
        public bool complex = false;
        public float complexChance = 25f;

        //These are not set by the user
        public static bool HARActive = false;
        public static bool RotRActive = false;
        public static bool ATRActive = false;
        public static bool VREHighmateActive = false;
        public static bool VREAndroidActive = false;
        public static bool NonBinaryActive = false;
        public static Dictionary<string, string> FertilityMods = new();
        public static bool debugLogging = false;
        public static bool AsimovActive;
        public static bool PawnmorpherActive;
        public static bool TransActive;
        public static bool AltFertilityActive;
        public static NeedDef JoyNeed;

        public static bool LoveRelationsLoaded => !CustomLoveRelationUtility.LoveRelations.EnumerableNullOrEmpty();
        public static List<RaceSettings> RaceSettingsList = new();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref sexualOrientations, "sexualOrientations");
            //This is for converting old config files
            if (sexualOrientations is null)
            {
                sexualOrientations = new();
                Scribe_Values.Look(ref sexualOrientations.standardOrientation.none, "asexualChance", 10.0f);
                Scribe_Values.Look(ref sexualOrientations.standardOrientation.bi, "bisexualChance", 50.0f);
                Scribe_Values.Look(ref sexualOrientations.standardOrientation.homo, "gayChance", 20.0f);
                Scribe_Values.Look(ref sexualOrientations.standardOrientation.hetero, "straightChance", 20.0f);
                sexualOrientations.orientationForMen.CopyFrom(sexualOrientations.standardOrientation);
                sexualOrientations.orientationForWomen.CopyFrom(sexualOrientations.standardOrientation);
                sexualOrientations.orientationForEnby.CopyFrom(sexualOrientations.standardOrientation);
            }
            Scribe_Deep.Look(ref romanticOrientations, "romanticOrientations");
            if (romanticOrientations is null)
            {
                romanticOrientations = new();
                Scribe_Values.Look(ref romanticOrientations.standardOrientation.none, "aromanticChance", 10.0f);
                Scribe_Values.Look(ref romanticOrientations.standardOrientation.bi, "biromanticChance", 50.0f);
                Scribe_Values.Look(ref romanticOrientations.standardOrientation.homo, "homoromanticChance", 20.0f);
                Scribe_Values.Look(ref romanticOrientations.standardOrientation.hetero, "heteroromanticChance", 20.0f);
                romanticOrientations.orientationForMen.CopyFrom(romanticOrientations.standardOrientation);
                romanticOrientations.orientationForWomen.CopyFrom(romanticOrientations.standardOrientation);
                romanticOrientations.orientationForEnby.CopyFrom(romanticOrientations.standardOrientation);
            }

            Scribe_Values.Look(ref dateRate, "dateRate", 100.0f);
            Scribe_Values.Look(ref hookupRate, "hookupRate", 100.0f);
            Scribe_Values.Look(ref alienLoveChance, "alienLoveChance", 33.0f);
            Scribe_Values.Look(ref minOpinionRomance, "minOpinionRomance", 5);
            Scribe_Values.Look(ref cheatChance, "cheatChance", 100.0f);
            Scribe_Values.Look(ref minOpinionHookup, "minOpinionHookup", 0);
            Scribe_Values.Look(ref cheatingOpinion, "cheatingOpinion", new(-75, 75));

            Scribe_Values.Look(ref fertilityMod, "fertilityMod", "None");
            Scribe_Values.Look(ref joyOnSlaves, "joyOnSlaves", false);
            Scribe_Values.Look(ref joyOnPrisoners, "joyOnPrisoners", false);
            Scribe_Values.Look(ref joyOnGuests, "joyOnGuests", false);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref complex, "compexOrientations", false);
            Scribe_Values.Look(ref complexChance, "complexChance", 25f);
        }

        public static void ApplyJoySettings()
        {
            JoyNeed ??= DefDatabase<NeedDef>.GetNamed("Joy", false);
            JoyNeed.neverOnSlave = !BetterRomanceMod.settings.joyOnSlaves;
            if (BetterRomanceMod.settings.joyOnPrisoners)
            {
                JoyNeed.neverOnPrisoner = false;
                JoyNeed.colonistAndPrisonersOnly = true;
                JoyNeed.colonistsOnly = false;
            }
            else
            {
                JoyNeed.neverOnPrisoner = true;
                JoyNeed.colonistAndPrisonersOnly = false;
                JoyNeed.colonistsOnly = true;
                BetterRomanceMod.settings.joyOnGuests = false;
            }
        }

        //Try to auto set if there's only one choice
        public static void AutoDetectFertilityMod()
        {
            if (FertilityMods.Count == 1 && (fertilityMod == "None" || !FertilityMods.ContainsKey(fertilityMod)))
            {
                fertilityMod = FertilityMods.First().Key;
            }
            else if (!FertilityMods.ContainsKey(fertilityMod))
            {
                fertilityMod = "None";
            }
        }

        private UIButtonText ComplexButton()
        {
            return NewElement.Button(() => complex = !complex)
                .WithLabel(() => (complex ? "WBR.Simplify" : "WBR.Complicated").Translate());
        }

        private void FertilityModOnClick()
        {
            List<FloatMenuOption> options = new();
            foreach (KeyValuePair<string, string> item in FertilityMods)
            {
                options.Add(new FloatMenuOption(item.Value, delegate
                {
                    fertilityMod = item.Key;
                }));
            }
            if (!options.NullOrEmpty())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}