using UnityEngine;

namespace Bloombots.Gameplay
{
    public sealed partial class EndlessArena
    {
        private const int TitanWaveInterval = 5;
        private const float TitanFuse = 3.5f;
        private const int TitanBlastLimit = 3;
        private static readonly Color32 TitanDanger = new(248, 57, 27, 212);
        private static readonly Color TitanImpact = new(1, .43f, .13f);
        private static readonly Vector2Int[] TitanBlastOffsets =
        {
            Vector2Int.zero, Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new(-1, 1), new(1, 1), new(-1, -1), new(1, -1)
        };

        private ArenaActor rustTitan;
        private float titanAftermathUntil;
        private bool IsTitanWave => !storyRun && dayNumber % TitanWaveInterval == 0;

        private void LandRustTitan()
        {
            var landing = new Vector2Int(ArenaBoard.Lanes / 2, ArenaBoard.Rows / 2);
            int best = 0;
            for (int row = 0; row < ArenaBoard.Rows; row++)
            for (int lane = 0; lane < ArenaBoard.Lanes; lane++)
            {
                var center = new Vector2Int(lane, row);
                int count = 0;
                foreach (var offset in TitanBlastOffsets)
                    if (TitanDefenderAt(center + offset) != null)
                        count++;
                // Favor occupied landing plots when two areas have the same number of defenders.
                int score = count * 2 + (TitanDefenderAt(center) != null ? 1 : 0);
                if (score <= best)
                    continue;
                best = score;
                landing = center;
            }

            rustTitan = Rent(BoltBrute, landing.x, landing.y + .5f, boss: true);
            rustTitan.Cell = landing;
            rustTitan.Fuse = TitanFuse;
            look.Accent();
            Burst(GroundPoint(rustTitan), TitanImpact, .9f, .55f);
        }

        private ArenaActor TitanDefenderAt(Vector2Int cell) =>
            ArenaBoard.Contains(cell.x, cell.y) && units[cell.x, cell.y] is { Alive: true } defender
                ? defender : null;

        private bool IsTitanDanger(Vector2Int cell) => rustTitan != null
            && Mathf.Abs(cell.x - rustTitan.Cell.x) <= 1 && Mathf.Abs(cell.y - rustTitan.Cell.y) <= 1;

        private void TickRustTitan(ArenaActor titan, float dt)
        {
            titan.Fuse = Mathf.Max(0, titan.Fuse - dt);
            if (titan.Fuse > 0)
                return;

            int defeated = 0;
            foreach (var offset in TitanBlastOffsets)
            {
                var cell = titan.Cell + offset;
                if (!ArenaBoard.Contains(cell.x, cell.y))
                    continue;
                Burst(board.World(cell.x + .5f, cell.y + .5f), TitanImpact, .41f, .59f);
                var defender = TitanDefenderAt(cell);
                if (defender == null || defeated >= TitanBlastLimit)
                    continue;
                Defeat(defender);
                defeated++;
            }
            Burst(GroundPoint(titan), TitanImpact, 1.15f, .62f);
            Defeat(titan);
            Say(defeated == 0 ? "Rust Titan detonated. No defenders lost."
                : $"Rust Titan detonated. Defenders lost: {defeated}.");
        }
    }
}
