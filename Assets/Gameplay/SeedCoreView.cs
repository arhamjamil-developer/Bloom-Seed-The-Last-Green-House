using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Bloombots.Gameplay
{
    public sealed class SeedCoreView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer shell;
        [SerializeField] private Sprite healthy;
        [SerializeField] private Sprite[] damageStages;
        [SerializeField] private Light2D glow;
        [SerializeField] private LineRenderer[] cables;
        [SerializeField] private float damagedWidth = 1.08f;

        private Vector3 healthyPosition, healthyScale;
        private int health;
        private float impact;
        private bool initialized;

        public void SetHealth(int value, bool hit)
        {
            if (!initialized)
            {
                healthyPosition = shell.transform.localPosition;
                healthyScale = shell.transform.localScale;
                initialized = true;
            }
            health = value;
            int stage = value switch { 20 => -1, >= 15 => 0, >= 8 => 1, >= 1 => 2, _ => 3 };
            Sprite damaged = stage >= 0 ? damageStages[stage] : null;
            shell.sprite = damaged != null ? damaged : healthy;
            shell.transform.localPosition = damaged != null ? Vector3.zero : healthyPosition;
            shell.transform.localScale = damaged != null ? Vector3.one * (damagedWidth / damaged.bounds.size.x) : healthyScale;
            impact = hit ? 1 : 0;
        }

        internal void Draw(ArenaBoard board, ArenaActor[,] units, float clock, float dt, float flash, float night)
        {
            impact = Mathf.MoveTowards(impact, 0, dt * 3.9f);
            float strength = health / 20f;
            float shake = Mathf.Sin(impact * 35) * impact * .033f * flash;
            shell.transform.localPosition = (shell.sprite == healthy ? healthyPosition : Vector3.zero) + Vector3.right * shake;
            shell.color = Color.Lerp(Color.white, new Color(1, .58f, .35f), impact * flash);
            Color power = Color.Lerp(new Color(1, .42f, .09f), new Color(.49f, 1, .36f), strength);
            if (health > 14)
                power = new Color(.35f, 1, .69f);
            glow.color = power;
            glow.intensity = health == 0 ? 0 : Mathf.Lerp(.20f, .96f, night) * (.25f + strength * .75f);

            for (int lane = 0; lane < cables.Length; lane++)
            {
                var wire = cables[lane];
                int farthest = ArenaBoard.Rows;
                for (int row = 0; row < ArenaBoard.Rows; row++)
                {
                    var plant = units[lane, row];
                    if (plant != null && plant.Alive && !plant.Spec.Hero)
                    {
                        farthest = row;
                        break;
                    }
                }
                wire.enabled = farthest < ArenaBoard.Rows;
                if (!wire.enabled)
                    continue;

                Vector3 outlet = wire.transform.position;
                Vector3 trunk = board.World(lane + .5f, ArenaBoard.Rows - .03f);
                Vector3 end = board.World(lane + .5f, farthest + .5f);
                Vector3 bend = new(trunk.x, outlet.y + .035f, 0);
                wire.SetPosition(0, health == 0 ? Vector3.Lerp(outlet, bend, .32f) + Vector3.up * .03f : outlet);
                wire.SetPosition(1, Vector3.Lerp(outlet, bend, .64f));
                wire.SetPosition(2, Vector3.Lerp(bend, trunk, .35f));
                wire.SetPosition(3, trunk);
                wire.SetPosition(4, end);
                float pulse = .87f + Mathf.Sin(clock * 5 - lane * .68f) * .13f * flash;
                wire.startColor = wire.endColor = health == 0 ? new Color(.17f, .13f, .10f) : power * (pulse * (.42f + strength * .58f));
            }
        }
    }
}
