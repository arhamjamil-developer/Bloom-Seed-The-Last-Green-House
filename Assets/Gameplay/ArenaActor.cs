using UnityEngine;

namespace Bloombots.Gameplay
{
    internal enum Role { Guard, Tether, Shot, Mortar, Frost, Heal, Beetle, Brute, Moth, Mole }
    internal enum Pose { Idle, Move, Side, Attack, Hurt, Defeat }

    internal sealed class UnitSpec
    {
        public readonly string Id;
        public readonly Role Role;
        public readonly float Hp, Damage, Cooldown, Range;
        public readonly bool Hero, NightReady, Enemy;

        public UnitSpec(string id, Role role, float hp, float damage, float cooldown, float range,
            bool hero = false, bool nightReady = false, bool enemy = false)
        {
            Id = id;
            Role = role;
            Hp = hp;
            Damage = damage;
            Cooldown = cooldown;
            Range = range;
            Hero = hero;
            NightReady = nightReady;
            Enemy = enemy;
        }

        private static readonly UnitSpec Beetle = new("scrap-beetle", Role.Beetle, 38, 12, 1.15f, .55f, enemy: true);
        private static readonly UnitSpec Brute = new("bolt-brute", Role.Brute, 98, 25, 1.65f, .64f, enemy: true);
        private static readonly UnitSpec Moth = new("dust-moth", Role.Moth, 47, 13, 1.05f, .52f, enemy: true);
        private static readonly UnitSpec Mole = new("drill-mole", Role.Mole, 72, 19, 1.35f, .54f, enemy: true);

        private UnitSpec WithAppearance(string id) => new(id, Role, Hp, Damage, Cooldown, Range, Hero, NightReady, Enemy);

        public static int EnemyAppearance(int original) => original switch
        {
            9 => 15,
            10 => 13,
            11 => 14,
            12 => 16,
            _ => original
        };

        // Order matches ArenaAssets.sheets.
        public static readonly UnitSpec[] All =
        {
            new("dubu", Role.Guard, 90, 18, 1.3f, 1.1f, hero: true),
            new("bubu", Role.Tether, 58, 11, 2.1f, 4.7f, hero: true, nightReady: true),
            new("arham", Role.Shot, 62, 23, 1.05f, 9, hero: true),
            new("jojo", Role.Mortar, 72, 29, 2.5f, 8, hero: true, nightReady: true),
            new("cactus-guard", Role.Guard, 115, 12, 1.4f, .9f),
            new("pea-pulser", Role.Shot, 46, 16, 1.2f, 9),
            new("lotus-frost", Role.Frost, 55, 8, 2.6f, 8, nightReady: true),
            new("mushroom-mortar", Role.Mortar, 55, 24, 2.45f, 8),
            new("sunflower-healer", Role.Heal, 52, 17, 2.8f, 1.45f, nightReady: true),
            Beetle, Brute, Moth, Mole,
            Brute.WithAppearance("bronze-rampart"),
            Moth.WithAppearance("coil-wasp"),
            Beetle.WithAppearance("ruby-skitter"),
            Mole.WithAppearance("kiln-borer")
        };
    }

    internal sealed class ArenaActor
    {
        public readonly SpriteRenderer Renderer;
        private readonly SpriteRenderer shadow;
        public readonly SpriteRenderer HealthTrack;
        public readonly SpriteRenderer HealthFill;
        public UnitSpec Spec;
        public int Kind, Serial, TargetSerial;
        public Vector2Int Cell;
        public float Lane, Row, Hp, MaxHp, AttackIn, Windup, Slow, MoveRemaining, Retire, Tier;
        public float PoseTime, Scale, BaseHeight;
        public float Fuse;
        public bool Alive, Boss, UsedTrick;
        public Vector2 MoveFrom;
        public ArenaActor Target;
        public Pose Pose;
        public Vector3 Ground => Renderer.transform.position;

        public ArenaActor(Transform parent, Material material, Material effectsMaterial, Sprite pixel, Sprite shadowSprite)
        {
            Renderer = new GameObject("Actor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            Renderer.transform.SetParent(parent, false);
            Renderer.sharedMaterial = material;
            shadow = Bar("Contact shadow", Color.white, effectsMaterial, shadowSprite);
            HealthTrack = Bar("Health track", new Color(.15f, .09f, .07f), effectsMaterial, pixel);
            HealthFill = Bar("Health fill", Color.white, effectsMaterial, pixel);
        }

        private SpriteRenderer Bar(string name, Color color, Material material, Sprite pixel)
        {
            var bar = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            bar.transform.SetParent(Renderer.transform, false);
            bar.sharedMaterial = material;
            bar.sprite = pixel;
            bar.color = color;
            return bar;
        }

        public void Play(Pose pose)
        {
            if (Pose == pose)
                return;
            Pose = pose;
            PoseTime = 0;
        }

        public int FrameRow()
        {
            if (Spec.Hero)
                return Pose switch { Pose.Move => 1, Pose.Side => 2, Pose.Attack => 3, Pose.Hurt => 4, Pose.Defeat => 5, _ => 0 };
            if (Spec.Enemy)
                return Pose switch { Pose.Move => 1, Pose.Attack => 2, Pose.Hurt => 3, Pose.Defeat => 4, _ => 0 };
            return Pose switch { Pose.Attack => 1, Pose.Hurt => 2, Pose.Defeat => 3, _ => 0 };
        }

        public void Draw(ArenaBoard board, Sprite[] frames, float dt, bool guarding, float cadence, float lift)
        {
            PoseTime += dt;
            int frame = Pose is Pose.Idle or Pose.Move or Pose.Side
                ? (int)(PoseTime * (Pose == Pose.Idle ? 4 : 11)) % 4
                : Mathf.Min(3, (int)(PoseTime * (Pose == Pose.Attack ? 4 / cadence : 9)));
            int row = guarding && Pose == Pose.Idle ? 4 : FrameRow();
            if (!Boss)
                Renderer.sprite = frames[row * 4 + frame];
            Vector3 ground = board.World(Lane + .5f, Row) + Vector3.up * lift;
            if (Spec.Role == Role.Moth && Alive)
                ground.y += .12f + Mathf.Sin(PoseTime * 10) * .035f;
            Renderer.transform.position = ground;
            float scale = Scale * ArenaBoard.DepthScale(Row);
            Renderer.transform.localScale = Vector3.one * scale;
            int order = 10 + Mathf.RoundToInt(Row * 20);
            Renderer.sortingOrder = order;
            shadow.sortingOrder = order - 1;
            shadow.transform.localPosition = (board.World(Lane + .5f, Row) - ground) / scale;
            shadow.transform.localScale = new Vector3(BaseHeight * .95f, BaseHeight * .36f, 1);
            shadow.color = new Color(1, 1, 1, Alive ? 1 : Renderer.color.a);

            bool showBar = Alive && (Spec.Enemy || Hp < MaxHp);
            HealthTrack.enabled = HealthFill.enabled = showBar;
            if (!showBar)
                return;

            float ratio = Mathf.Clamp01(Hp / MaxHp);
            float barWidth = BaseHeight * .60f;
            float barY = BaseHeight * 1.05f;
            HealthTrack.transform.localPosition = new Vector3(0, barY, 0);
            HealthTrack.transform.localScale = new Vector3(barWidth, .052f, 1);
            HealthTrack.sortingOrder = 255 + order;
            HealthFill.transform.localPosition = new Vector3(-barWidth * (1 - ratio) * .5f, barY, 0);
            HealthFill.transform.localScale = new Vector3(barWidth * ratio, .032f, 1);
            HealthFill.sortingOrder = HealthTrack.sortingOrder + 1;
            HealthFill.color = Spec.Enemy ? new Color(1, .23f, .13f) : new Color(.35f, 1, .5f);
        }
    }
}

