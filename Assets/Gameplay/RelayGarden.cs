using UnityEngine;

namespace Bloombots
{
    internal static class RelayGarden
    {
        private const string RestoredKey = "Bloombots.RestoredGardens";
        public static readonly string[] Names = { "Dawn Nursery", "Moonlit Canopy", "Rustworks Gate", "Seedheart Siege" };
        public static readonly string[] Briefs =
        {
            "Learn to defend the Seed Core, move heroes between lanes, and connect your plants.",
            "Fast fliers slip past the first line. Use night-ready defenders and time your visor.",
            "Armored bugs push through the outer lanes while burrowers damage your plots.",
            "Hold the final relay against a mixed swarm and Rust Titan."
        };

        private static int Restored => PlayerPrefs.GetInt(RestoredKey)
            | (PlayerPrefs.GetInt(BloomPreferences.DawnRestored) != 0 ? 1 : 0);

        public static bool IsRestored(int index) => (Restored & (1 << index)) != 0;

        public static int RestoredCount()
        {
            int count = 0;
            for (int i = 0; i < Names.Length; i++)
                if (IsRestored(i))
                    count++;
            return count;
        }

        public static void Restore(int index)
        {
            PlayerPrefs.SetInt(RestoredKey, Restored | (1 << index));
            if (index == 0)
                PlayerPrefs.SetInt(BloomPreferences.DawnRestored, 1);
            PlayerPrefs.Save();
        }
    }
}
