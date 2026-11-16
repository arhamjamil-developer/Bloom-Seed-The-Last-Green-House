using UnityEngine;

namespace Bloombots.Gameplay
{
    public sealed partial class EndlessArena
    {
        private const int GardenWaveCount = 3;
        private const int ScrapBeetle = 9;
        private const int BoltBrute = 10;
        private const int DustMoth = 11;
        private const int DrillMole = 12;
        private const int BronzeRampart = 13;
        private const int CoilWasp = 14;
        private const int RubySkitter = 15;
        private const int KilnBorer = 16;

        private static readonly (int Lane, int Kind)[][] RelayWaves =
        {
            new[] { (2, DustMoth), (2, CoilWasp), (3, ScrapBeetle), (3, DustMoth) },
            new[] { (1, CoilWasp), (3, DustMoth), (1, RubySkitter), (2, BoltBrute), (3, CoilWasp) },
            new[] { (0, DustMoth), (4, CoilWasp), (0, BoltBrute), (4, RubySkitter), (2, CoilWasp), (2, DrillMole) },
            new[] { (1, RubySkitter), (3, BoltBrute), (1, DrillMole), (3, BronzeRampart) },
            new[] { (0, BoltBrute), (4, BronzeRampart), (0, KilnBorer), (4, DrillMole), (2, RubySkitter) },
            new[] { (0, BronzeRampart), (4, BoltBrute), (2, KilnBorer), (0, RubySkitter), (4, DrillMole), (2, BronzeRampart) },
            new[] { (1, ScrapBeetle), (3, RubySkitter), (2, BoltBrute), (1, CoilWasp), (3, KilnBorer) },
            new[] { (0, CoilWasp), (4, DustMoth), (2, BronzeRampart), (0, KilnBorer), (4, RubySkitter), (2, DrillMole) },
            new[] { (0, RubySkitter), (4, CoilWasp), (1, BronzeRampart), (3, KilnBorer), (2, BoltBrute), (2, DustMoth), (4, DrillMole) }
        };

        private void MakeGardenSchedule()
        {
            if (storyGarden == 0)
            {
                MakeDawnSchedule();
                return;
            }

            var wave = RelayWaves[(storyGarden - 1) * GardenWaveCount + dayNumber - 1];
            for (int i = 0; i < wave.Length; i++)
                AddDawnSpawn(wave[i].Lane, wave[i].Kind, .7f + i * 3.1f);
            if (storyGarden == 3 && dayNumber == GardenWaveCount)
                spawns.Add(new Spawn { Boss = true });
        }

        private void MakeDawnSchedule()
        {
            switch (dayNumber)
            {
                case 1:
                    AddDawnSpawn(2, ScrapBeetle, .7f);
                    AddDawnSpawn(2, ScrapBeetle, 4.8f);
                    AddDawnSpawn(2, ScrapBeetle, 8.5f);
                    AddDawnSpawn(2, BoltBrute, 11f);
                    break;
                case 2:
                    AddDawnSpawn(2, ScrapBeetle, .7f);
                    AddDawnSpawn(3, ScrapBeetle, 5.5f);
                    AddDawnSpawn(3, ScrapBeetle, 9.2f);
                    AddDawnSpawn(2, BoltBrute, 13f);
                    break;
                case 3:
                    AddDawnSpawn(1, ScrapBeetle, .7f);
                    AddDawnSpawn(2, ScrapBeetle, 2.1f);
                    AddDawnSpawn(3, ScrapBeetle, 5f);
                    AddDawnSpawn(2, BoltBrute, 8.2f);
                    AddDawnSpawn(1, ScrapBeetle, 10.3f);
                    AddDawnSpawn(3, BoltBrute, 14f);
                    break;
            }
        }

        private void AddDawnSpawn(int lane, int kind, float at) =>
            spawns.Add(new Spawn { Lane = lane, Kind = kind, At = at });

        private void RestoreGarden()
        {
            phase = Phase.StoryCleared;
            ClearSelection();
            selected = null;
            RelayGarden.Restore(storyGarden);
            look.Accent();
            hud.StoryComplete(RelayGarden.Names[storyGarden]);
        }
    }
}
