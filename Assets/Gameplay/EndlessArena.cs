using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bloombots.Gameplay
{
    [DisallowMultipleComponent]
    public sealed partial class EndlessArena : MonoBehaviour
    {
        private enum Phase { Plan, Defend, Harvest, Lost, StoryCleared }
        private enum Blight { None, QuickSwarm, IronShells, SunDrought }
        private const float PlanSeconds = 8;
        private const float AttackSeconds = .46f;
        private const float AttackCue = AttackSeconds * .5f;
        private const int CoreMax = 20;
        private const int ActorLimit = 48;
        private const float CoreRow = 8.95f;

        private static readonly Color32 EmptyPlot = new(0, 0, 0, 0);
        private static readonly Color32 IncomingPlot = new(244, 150, 38, 120);
        private static readonly Color32 LegalPlot = new(122, 210, 87, 136);
        private static readonly Color32 LinkedPlot = new(232, 199, 65, 117);
        private static readonly Color32 BlockedPlot = new(232, 76, 41, 184);
        private static readonly Color32 AimPlot = new(102, 241, 168, 234);
        private static readonly Color32 SelectedPlot = new(92, 205, 247, 216);
        private static readonly string[] UpgradeText =
        {
            "SHARP SHOOTS  ·  +15% attack damage",
            "WARM ROOTS  ·  Heal 4 Seed Core leaves",
            "SUN CACHE  ·  Gain 8 Sun",
            "QUICK GROWTH  ·  Faster card recharge",
            "LIVING ARMOR  ·  +15% defender health"
        };

        private sealed class CardSlot
        {
            public BloomCardView View;
            public int Kind, LastSun = -1;
            public bool LastLegal;
            public bool LastGuideLocked;
            public float ReadyAt, LastRemaining = -1;
        }

        private struct Spawn
        {
            public int Lane, Kind;
            public bool Boss;
            public float At;
        }

        private readonly List<CardSlot> cards = new();
        private readonly Dictionary<BloomCardView, CardSlot> cardMap = new();
        private readonly ArenaActor[,] units = new ArenaActor[ArenaBoard.Lanes, ArenaBoard.Rows];
        private readonly float[,] brokenUntil = new float[ArenaBoard.Lanes, ArenaBoard.Rows];
        private readonly int[,] links = new int[ArenaBoard.Lanes, ArenaBoard.Rows];
        private readonly List<ArenaActor> actors = new();
        private readonly Stack<ArenaActor> actorPool = new();
        private readonly List<Spawn> spawns = new();
        private readonly bool[] incoming = new bool[ArenaBoard.Lanes];
        private readonly int[] choices = new int[3];
        private readonly List<Vector2Int> component = new(40);
        private readonly bool[,] visited = new bool[ArenaBoard.Lanes, ArenaBoard.Rows];
        private readonly List<Sprite> ownedSprites = new();

        [SerializeField] private ArenaAssets assets;
        [SerializeField] private SpriteRenderer backdrop;
        [SerializeField] private SeedCoreView core;
        [SerializeField] private MeshFilter gridSurface;
        [SerializeField] private SpriteRenderer[] pipes;
        private ArenaBoard board;
        private ArenaLook look;
        private ArenaEffects effects;
        private ArenaHud hud;
        private BottomBannerView banner;
        private Camera arenaCamera;
        private RectTransform sunTarget;
        private Button mergeButton;
        private Sprite[] props;
        private Sprite[][] frames;
        private float[] idleHeights;
        private Sprite pixel, shadow;
        private Texture2D pixelTexture, shadowTexture;
        private System.Random random;
        private Phase phase;
        private Blight blight;
        private CardSlot armed;
        private ArenaActor selected;
        private Vector2Int hover;
        private bool hoverValid, dragging, paused, night, visorOn, chainDirty, lastSprout;
        private bool leavingArena;
        private bool storyRun;
        private int storyGarden = -1;
        private int firstLane;
        private int dragPointer, dayNumber, sun, coreHp, bestDay, cursor, serial, pendingUpgrade;
        private int displayedSun = -1, displayedPips = -1;
        private float clock, phaseTime, nextSun, charge, attackPower, armor, recharge, nextLinkChange;
        private float flash = 1, motion = 1;
        private string notice;
        private float noticeUntil;

        private void Start()
        {
            arenaCamera = Camera.main;
            banner = FindAnyObjectByType<BottomBannerView>();
            if (arenaCamera == null || banner == null || assets == null || core == null || backdrop == null || gridSurface == null)
            {
                Debug.LogError("Assign the arena stage references and include a Main Camera and BottomBannerView.", this);
                enabled = false;
                return;
            }

            var atlas = JsonUtility.FromJson<ArenaAssets.Atlas>(assets.frames.text);
            frames = new Sprite[UnitSpec.All.Length][];
            idleHeights = new float[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                var sheet = atlas.sheets[i];
                var texture = assets.sheets[i];
                frames[i] = new Sprite[sheet.frames.Length];
                for (int j = 0; j < frames[i].Length; j++)
                    frames[i][j] = Own(sheet.frames[j].Build(texture));
                idleHeights[i] = sheet.idleHeight * texture.height / 100;
            }
            props = new Sprite[atlas.props.Length];
            for (int i = 1; i < props.Length; i++)
                props[i] = Own(atlas.props[i].Build(assets.props));

            pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixelTexture.SetPixel(0, 0, Color.white);
            pixelTexture.Apply();
            pixel = Own(Whole(pixelTexture, new Vector2(.5f, .5f), 1));
            shadowTexture = MakeShadowTexture();
            shadow = Own(Whole(shadowTexture, new Vector2(.5f, .5f), shadowTexture.width));
            board = new ArenaBoard(gridSurface, arenaCamera);
            effects = new ArenaEffects(transform, assets);

            motion = Mathf.Clamp01(PlayerPrefs.GetFloat(BloomPreferences.Motion, 1));
            flash = Mathf.Clamp01(PlayerPrefs.GetFloat(BloomPreferences.Flash, 1));
            bestDay = PlayerPrefs.GetInt(BloomPreferences.BestDay, 0);
            look = new ArenaLook(transform, arenaCamera);
            sunTarget = banner.transform.Find("SafeArea/BottomBanner/SunCounter") as RectTransform;
            mergeButton = banner.transform.Find("SafeArea/BottomBanner/MergeButton")?.GetComponent<Button>();
            if (mergeButton != null)
                mergeButton.onClick.AddListener(MergeSelected);
            foreach (var card in banner.GetComponentsInChildren<BloomCardView>(true))
            {
                int kind = Array.FindIndex(UnitSpec.All, entry => entry.Id == card.CardId);
                if (kind < 0 || UnitSpec.All[kind].Enemy)
                    continue;

                var slot = new CardSlot { View = card, Kind = kind };
                cards.Add(slot);
                cardMap.Add(card, slot);
                card.OnPlacementRequested.AddListener(Arm);
                card.gameObject.AddComponent<ArenaCardDrag>().Bind(this, card);
            }
            hud = new ArenaHud(banner, this, assets);
            storyGarden = MainMenuController.TakeStoryRequest();
            storyRun = storyGarden >= 0;
            look.SetGarden(storyGarden);
            PrepareGuide();
            Canvas.ForceUpdateCanvases();
            FitArena();
            RestartRun();
        }

        private void FitArena()
        {
            if (!board.Fit(hud.ArenaViewport()))
                return;

            core.transform.position = board.World(2.5f, CoreRow);
            for (int lane = 0; lane < pipes.Length; lane++)
                pipes[lane].transform.position = board.World(lane + .5f, -1.55f);
            if (paused || phase is Phase.Lost or Phase.StoryCleared)
                DrawActors(0);
        }

        private void Update()
        {
            if (board == null || hud == null)
                return;
            FitArena();
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (!paused && phase is not (Phase.Lost or Phase.StoryCleared))
            {
                if (!GuideBlocksWave)
                {
                    clock += dt;
                    phaseTime += dt;
                    if (phase is Phase.Plan or Phase.Defend)
                        TickDay(dt);
                    else if (phase == Phase.Harvest && phaseTime >= 4 && pendingUpgrade >= 0)
                        CompleteHarvest();
                }

                if (phase is not (Phase.Lost or Phase.StoryCleared))
                {
                    effects.Tick(dt);
                    DrawActors(dt);
                }
            }
            float grade = look.Tick(dt, night, visorOn, motion);
            core.Draw(board, units, clock, paused ? 0 : dt, flash, grade);
            if (phase == Phase.Lost && phaseTime < 1.1f)
            {
                phaseTime += dt;
                effects.Tick(dt, resolveHits: false);
                DrawActors(dt);
                if (phaseTime >= 1.1f)
                    hud.Lost(dayNumber - 1, bestDay, storyRun);
            }
            // Widen the illustrated path slightly on tablets so the outer plots stay over the soil.
            float artWidth = ArenaBoard.Height * assets.backdrop.width / assets.backdrop.height;
            float worldHeight = arenaCamera.orthographicSize * 2;
            float cover = Mathf.Max(1, Mathf.Max(worldHeight / ArenaBoard.Height, worldHeight * arenaCamera.aspect / artWidth));
            float widthScale = Mathf.Lerp(1, 1.17f, Mathf.InverseLerp(.53f, .75f, arenaCamera.aspect));
            float backdropScale = ArenaBoard.Height / backdrop.sprite.bounds.size.y * cover;
            backdrop.transform.localScale = new Vector3(backdropScale * widthScale, backdropScale, 1);
            PaintBoard(grade);
            RefreshHud();
        }

        private void LateUpdate()
        {
            if (dragging)
                RefreshDragPreview();
        }

        private void TickDay(float dt)
        {
            if (night)
            {
                charge = Mathf.Clamp01(charge + dt * (visorOn ? -.10f : .038f));
                if (charge <= 0)
                    visorOn = false;
            }
            if (clock >= nextSun)
            {
                sun++;
                nextSun = clock + (Season == 0 ? 5 : 7) * (blight == Blight.SunDrought ? 1.7f : 1);
            }
            TickUnits(dt);
            if (phase == Phase.Lost)
                return;
            if (phase == Phase.Plan && phaseTime >= PlanSeconds)
            {
                phase = Phase.Defend;
                phaseTime = 0;
                if (IsTitanWave)
                    look.Accent();
            }
            if (phase != Phase.Defend)
                return;

            if (cursor < spawns.Count && phaseTime >= spawns[cursor].At && ActiveEnemyCount() < ActorLimit)
            {
                var item = spawns[cursor];
                if (!item.Boss)
                {
                    cursor++;
                    var actor = Rent(item.Kind, item.Lane, -1.28f, boss: false);
                    actor.Play(Pose.Move);
                    Burst(board.World(item.Lane + .5f, -1.12f), new Color(1, .37f, .14f), .34f);
                }
                else if (ActiveEnemyCount() == 0)
                {
                    cursor++;
                    LandRustTitan();
                }
            }
            if (cursor == spawns.Count && ActiveEnemyCount() == 0 && clock >= titanAftermathUntil)
                StartHarvest();
        }

        private int Season => (dayNumber - 1) / 10 % 4;
        private int ActiveEnemyCount()
        {
            int count = 0;
            foreach (var actor in actors)
                if (actor.Alive && actor.Spec.Enemy)
                    count++;
            return count;
        }

        public void RestartRun()
        {
            foreach (var actor in actors)
                Release(actor);
            actors.Clear();
            rustTitan = null;
            titanAftermathUntil = 0;
            effects.Clear();
            Array.Clear(units, 0, units.Length);
            Array.Clear(brokenUntil, 0, brokenUntil.Length);
            Array.Clear(links, 0, links.Length);
            random = new System.Random(Environment.TickCount);
            clock = 0;
            nextSun = 6;
            sun = 12;
            banner.ResetSunMeter(sun);
            displayedSun = -1;
            dayNumber = 1;
            SetCoreHealth(CoreMax);
            attackPower = armor = recharge = 1;
            charge = 1;
            lastSprout = false;
            foreach (var card in cards)
                card.ReadyAt = 0;
            paused = false;
            selected = null;
            ClearSelection();
            StartDay();
            StartGuideIfNeeded();
            RefreshHud();
        }

        private void StartDay()
        {
            phase = Phase.Plan;
            phaseTime = 0;
            cursor = 0;
            visorOn = false;
            night = storyRun ? storyGarden == 1 || storyGarden == 3 && dayNumber == 2
                : dayNumber % 3 == 0 || Season == 3;
            blight = dayNumber >= 31 && dayNumber % 4 != 0 ? (Blight)random.Next(1, 4) : Blight.None;
            charge = 1;
            noticeUntil = 0;
            selected = null;
            ClearSelection();
            hud.HideDialog();
            MakeSchedule();
            chainDirty = true;
        }

        private void MakeSchedule()
        {
            spawns.Clear();
            if (storyRun)
            {
                MakeGardenSchedule();
                MarkIncoming();
                return;
            }

            int[] order = { 2, 1, 3, 0, 4 };
            int lanes = dayNumber == 1 ? 1 : dayNumber < 4 ? 3 : 5;

            int d = Mathf.Min(dayNumber, 150);
            bool boss = IsTitanWave;
            float budget = (10 + 6 * d + .35f * d * d) * (!boss && dayNumber % 4 == 0 ? .68f : 1);
            int count = Mathf.Clamp(Mathf.RoundToInt(budget / 4.2f), 4, 52);
            float duration = Mathf.Lerp(17, 53, Mathf.Clamp01((dayNumber - 1) / 9f));
            for (int i = 0; i < count; i++)
            {
                int pick = random.Next(100);
                int kind = dayNumber >= 5 && pick < 13 ? 12
                    : dayNumber >= 3 && pick < 31 ? 11
                    : dayNumber >= 2 && pick < 49 ? 10 : 9;
                if (dayNumber >= 2 && (i + dayNumber) % 2 == 0)
                    kind = UnitSpec.EnemyAppearance(kind);
                spawns.Add(new Spawn { Lane = order[random.Next(lanes)], Kind = kind, At = .45f + duration * i / count });
            }
            if (boss)
                spawns.Add(new Spawn { Boss = true });
            MarkIncoming();
        }

        private void MarkIncoming()
        {
            Array.Clear(incoming, 0, incoming.Length);
            firstLane = spawns.Count > 0 ? spawns[0].Lane : -1;
            foreach (var spawn in spawns)
                if (!spawn.Boss)
                    incoming[spawn.Lane] = true;
        }

        private void StartHarvest()
        {
            if (guideStep == GuideStep.Fight)
                CompleteGuide();
            if (storyRun && dayNumber == GardenWaveCount)
            {
                RestoreGarden();
                return;
            }
            phase = Phase.Harvest;
            phaseTime = 0;
            pendingUpgrade = -1;
            ClearSelection();
            sun += 3 + Mathf.Min(5, dayNumber / 3);
            RecordBest(dayNumber);
            look.Accent();
            var available = new List<int> { 0, 1, 2, 3, 4 };
            var options = new string[3];
            for (int i = 0; i < choices.Length; i++)
            {
                int index = random.Next(available.Count);
                choices[i] = available[index];
                options[i] = UpgradeText[choices[i]];
                available.RemoveAt(index);
            }
            hud.Harvest(options);
        }

        public void ChooseUpgrade(int index)
        {
            if (phase != Phase.Harvest || pendingUpgrade >= 0 || index < 0 || index >= choices.Length)
                return;
            pendingUpgrade = choices[index];
            hud.HideDialog();
        }

        private void CompleteHarvest()
        {
            switch (pendingUpgrade)
            {
                case 0: attackPower *= 1.15f; break;
                case 1: SetCoreHealth(coreHp + 4); break;
                case 2: sun += 8; break;
                case 3: recharge = Mathf.Max(.38f, recharge * .88f); break;
                case 4:
                    armor *= 1.15f;
                    foreach (var actor in actors)
                    {
                        if (!actor.Alive || actor.Spec.Enemy)
                            continue;
                        actor.Hp += actor.MaxHp * .15f;
                        actor.MaxHp *= 1.15f;
                    }
                    break;
            }
            dayNumber++;
            StartDay();
        }

        private void RecordBest(int survived)
        {
            if (storyRun || survived <= bestDay)
                return;
            bestDay = survived;
            PlayerPrefs.SetInt(BloomPreferences.BestDay, bestDay);
            PlayerPrefs.Save();
        }

        public void TogglePause()
        {
            if (phase is Phase.Harvest or Phase.Lost or Phase.StoryCleared)
                return;
            paused = !paused;
            ClearSelection();
            RefreshGuideView();
            if (paused)
                hud.Pause();
            else
            {
                PlayerPrefs.Save();
                hud.HideDialog();
            }
        }

        public void ToggleVisor()
        {
            if (night && !paused && phase is (Phase.Plan or Phase.Defend) && (visorOn || charge >= .12f))
                visorOn = !visorOn;
        }

        public void ReturnToMainMenu()
        {
            if (leavingArena || (!paused && phase is not (Phase.Lost or Phase.StoryCleared)))
                return;
            leavingArena = true;
            if (phase == Phase.StoryCleared)
                MainMenuController.ShowRestorationOnLoad(storyGarden);
            PlayerPrefs.Save();
            SceneManager.LoadSceneAsync(MainMenuController.SceneName);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && hud != null && !paused && phase is (Phase.Plan or Phase.Defend))
                TogglePause();
        }

        private void OnDestroy()
        {
            foreach (var card in cards)
                if (card.View != null)
                    card.View.OnPlacementRequested.RemoveListener(Arm);
            if (mergeButton != null)
                mergeButton.onClick.RemoveListener(MergeSelected);
            board?.Dispose();
            look?.Dispose();
            effects?.Dispose();
            foreach (var sprite in ownedSprites)
                Destroy(sprite);
            if (pixelTexture != null)
                Destroy(pixelTexture);
            if (shadowTexture != null)
                Destroy(shadowTexture);
        }

        private Sprite Own(Sprite sprite)
        {
            ownedSprites.Add(sprite);
            return sprite;
        }

        private static Sprite Whole(Texture2D texture, Vector2 pivot, float ppu = 100) =>
            Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, ppu, 0, SpriteMeshType.FullRect);

        private static Texture2D MakeShadowTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Soft arena contact shadow",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + .5f, y + .5f) / (size * .5f) - Vector2.one;
                float fade = Mathf.Clamp01(1 - p.sqrMagnitude);
                pixels[y * size + x] = new Color32(44, 29, 12, (byte)(112 * fade * fade));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void SetCoreHealth(int value, bool hit = false)
        {
            int next = Mathf.Clamp(value, 0, CoreMax);
            bool damaged = hit && next < coreHp;
            coreHp = next;
            core.SetHealth(coreHp, damaged);
            if (damaged)
                BloomFeedback.CoreHit();
        }
    }
}
