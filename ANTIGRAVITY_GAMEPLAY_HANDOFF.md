# BLOOMBOTS — The Last Greenhouse

Unity mechanics handoff adapted from the nine-page design PDF. The goal is a playable, repeatable Endless run in `Assets/Scenes/SampleScene.unity`, using the existing art. The current project has a greenhouse backdrop and a swipeable card banner. Treat the board, combat, economy, animation hookup, and run progression as implementation work.

## Build the playable loop first

1. **Plan** for 10 seconds. Show the incoming lanes. The player can select a card, tap a legal tile to deploy it, or select a deployed hero and tap an adjacent empty tile to move it. Give Day 1 one active lane and a free Cactus Guard deployment.
2. **Defend** for a target of 65 seconds. Enemies spawn at the far end of their lanes. Heroes and plants attack automatically. The player can keep deploying and moving heroes. After the spawn schedule closes, finish the Day when its final enemy dies or reaches the Core. A Core at zero HP ends the run immediately.
3. **Harvest** for a minimum of five seconds. Award the Day reward and show three deterministic upgrade choices. Continue when the player picks one and the minimum time has elapsed. Keep survivors, Sun, upgrades, and Core HP between Days.
4. Increment the Day and repeat indefinitely. At defeat, show Days survived, best Day, earned Seeds, and a large Restart button. Restart rebuilds the run from its starting state. Pause freezes movement, attack windups, spawning, cooldowns, and Day timers.

Use a 5-column by 8-row logical board. Row 0 is farthest from the player. Units move toward row 0 and Rust Bugs toward the Core past row 7. A single board projection must drive the visible grid, tile taps, sprite feet, hit tests, and effect positions on both portrait iPhone and iPad. Start with one playable lane and expand after placement, targeting, and Core damage work. The greenhouse image is scenery. Keep collision and selection in board coordinates.

## Defenders and enemies

Use the current two-page hand for the first playable build. Heroes are first, and swiping or tapping Plants opens the second page. Use the existing compact Sun costs shown on the cards. Balance the wallet, rewards, and prices together before using the PDF's larger currency scale.

| Card | Sun | Behavior |
|---|---:|---|
| Dubu | 2 | Mobile blocker. Shield checks the adjacent bug in front and counters on contact. |
| Bubu | 2 | Casts a frost rope at one bug in range. The tether slows it for a configurable duration. |
| Arham | 3 | Fires a seed shot up his lane. |
| Jojo | 3 | Lobs a charge at a target tile. Impact damages a 3×3 area, clamped at board edges. |
| Cactus Guard | 2 | Anchored tank. Blocks its lane and returns thorn damage when struck. |
| Pea Pulser | 3 | Anchored shooter. Fires at the first valid bug up its lane. |
| Lotus Frost | 2 | Anchored controller. Slows bugs in its lane. |
| Mushroom Mortar | 3 | Anchored artillery. Lobs a bomb with 3×3 impact. |
| Sunflower Healer | 4 | Anchored support from the current card art. First-pass rule heals one damaged adjacent defender per pulse. |

**Scrap Beetle** walks and bites the first blocker. **Bolt Brute** is slower and tougher and telegraphs a clamp slam. **Dust Moth** flies past the first defender in its lane once and remains targetable by ranged and splash attacks. **Drill Mole** briefly burrows under a plot and temporarily breaks its adjacent links on emergence. Start with Beetle, add Brute, then introduce one special behavior per Day. Use a scaled Bolt Brute for the prototype boss until Rust Titan art exists.

An occupied tile blocks deployment. A hero moves one cardinal step into an empty tile with a configurable move interval. Reserve its destination until arrival. A valid deployment atomically validates the tile and availability, spends Sun once, occupies the tile, and begins recharge. Cancel, invalid taps, and swipes cost nothing. Bugs stop to attack blockers and damage the Seed Core only after reaching the goal. Apply damage at a hit cue, and release the occupancy once on defeat. Keep simulation state separate from animation.

## Links, merges, and Endless tuning

- **Garden chains.** Start with cardinally adjacent live Bloombots. Recalculate connected components after placement, defeat, merge, or a link break. The PDF gives the multiplier `1 + 0.10 × (chainSize − 1)`. Show linked neighbors and a gold accent at size six. Heroes keep their individual roles in the first pass.
- **Merge.** The PDF calls for three matching Bloombots in a chain. On Merge, highlight eligible trios, require a destination selected from those three tiles, then revalidate and replace the trio with one upgraded unit. Use its existing art with a readable temporary tier cue. The six pips can show the selected chain size, capped at six. They are feedback, not a second currency. Keep spending and occupancy changes atomic.
- **Director.** Use one seeded RNG per run for wave and upgrade choices. The PDF suggests `budget = 10 + 6d + 0.35d²`, health multiplier `1.06^d`, and speed multiplier `min(1 + 0.01d, 1.5)`. Expose the coefficients, active-enemy cap, and minimum spawn spacing. Queue spawns when the cap is hit. Every fourth Day is a breather, every tenth is a boss, and the boss takes priority when both apply. Rotate Seasons every ten Days. Announce one modifier in Plan after Day 30. Begin with a small wave roster and keep difficulty caps adjustable for long runs.
- **Progression.** Store best Day and earned Seeds in versioned local data at Harvest and defeat. Only award each milestone once. The PDF's story, world-map reveal, Seedbank unlocks, shared Daily Garden, eight-card deck with four drawn, extra units, and once-per-run specials are follow-on work. The PDF's Last Sprout grants a one-time rescue at three Core leaves. Add it after the core defeat flow works.

## HUD, sliders, and asset hookup

- **HUD.** Top safe area shows Day, phase countdown, Core HP out of 20, and Pause. Use a simple HP bar until leaf art exists. Preserve the bottom tray, hero/plant swipe, Sun display, Merge button, and per-card recharge shader. Give blocked placement a shape or text cue as well as color.
- **Player sliders.** Add master volume, music, SFX, camera shake, and flash intensity, each from 0 to 100%. Persist settings. Shake or flash at zero should remove that effect. Use temporary controls and optional audio placeholders.
- **Development sliders.** In an editor/development-only panel, expose simulation speed (0.25×–3×), spawn budget (0.5×–3×), bug HP (0.25×–4×), and Sun income (0×–3×). Keep defaults at 1× and show current values. Put base costs, Core HP, phase lengths, cooldowns, and scaling coefficients in Inspector-editable data assets. Spawn budget changes apply to the next generated schedule.
- **Existing UI.** Reuse `Assets/Prefabs/BloomCard.prefab` and `Assets/UI/BottomBanner/BottomBannerCanvas.prefab`. Subscribe to `BloomCardView.OnPlacementRequested`. Resolve by `CardId`, and update `SetAvailability(sun, remainingSeconds, hasLegalTile)` from live state. Push the wallet and pip count through `BottomBannerView.SetDisplay`. Keep the pager responsible for browsing.
- **Existing art.** Use `Assets/Environment/Greenhouse_Master_Plate.png`, `Greenhouse_Soil_Surface.png`, and `Greenhouse_Seed_Core.png`. Slice sprites in `Assets/Sprites` and align frames at the feet. Defender and enemy sheets face opposite directions. Human sheets have six rows of four frames (idle, jog, sidestep, action, hurt, defeat). Plants have five rows (idle, action, hurt, defeat, ready). Enemies have five (idle, movement, action, hurt, defeat). Manually inspect the gutters before import, especially Dust Moth wings and action effects. Use frame three of an action as the initial release cue where the art supports it. Runtime rope, projectiles, and splash checks should originate at per-unit sockets and land on logical targets. Use temporary simple effects and pool frequently spawned actors.

## Acceptance order

1. **One lane.** In Play Mode, deploy Cactus Guard, block Scrap Beetle, fire a seed, collect Sun, and lose when the Core reaches zero. Restart clears the board and timers.
2. **Full board.** Validate five lanes and eight rows, hero steps, swipe in both directions, every card's affordability and cooldown, and stable anchors on narrow portrait phone and iPad. UI gestures must not leak into board placement.
3. **Run loop.** Complete Plan → Defend → Harvest, choose an upgrade, and reach Day 11 in an accelerated test. Verify the boss, breather priority, pause/resume, restart, and saved best Day. Use a seeded Day 31 debug start to verify modifier announcements and that Endless continues.
4. **Strategy and polish.** Verify links break and reform, valid merges consume exactly three matching defenders, and each special enemy is counterable. Record actual Play Mode and device results separately. Preserve the approved art and request any missing final effects or visuals for a later art pass.
