# Bloom Heroes To Save Seed

## Game design, story, and production plan

**Design review · 10 November 2026 · Portrait iPhone and iPad**

### The goal

**Keep the last Seed alive long enough to wake four gardens and bring the city back to life.** On each board, the player plants a formation that grows stronger through its connections and moves human heroes to cover the lanes that formation leaves open. The best decisions ask the player where to commit roots and where to stay flexible.

Give the player three goals they can see at a glance. In combat, protect the Seed Core and read the next threat. In Story, restore the next relay garden and watch a district grow back. In Endless, push their Best Day and restore more of their expedition map. Each goal pays off on screen.

Build one polished stretch from the opening tutorial through the first restored garden. Use the current greenhouse, heroes, plants, tray, and bugs. That slice should prove that blocking, shooting, moving, and linking are readable and enjoyable before the team produces the rest of the campaign.

## 1. What the project does today

This review covers the serialized menu and arena, project gameplay and UI code, sprite and environment references, production concepts, and all nine pages of the original *BLOOMBOTS: The Last Greenhouse* PDF. The PDF is historical design input. The current human heroes, plant roster, and compact Sun economy take precedence where they differ. The findings below come from project files and art inspection. Play Mode behavior, touch feel, and device performance still need verification.

| Area | Implemented in project files | What to finish |
| --- | --- | --- |
| Entry and onboarding | `Main Menu` loads `SampleScene`. The menu offers Cards Deck, How to Play, replayable guided tutorial, and motion and flash settings. The guide pauses the clock, teaches two actual placements, and starts a fixed center-lane wave of four Scrap Beetles and a Bolt Brute. Skip returns to ordinary planning. | Teach plant linking, hero movement, Harvest, and night when each becomes useful. Turn the deck's illustrated role descriptions into live card details tied to current costs and behavior. |
| Board and controls | Five lanes by eight rows. The perspective mesh and inverse hit mapping share one projection. Drag raises the gameplay sprite, and tap-card-then-tap-plot is supported. The tray pages between Heroes and Plants. Heroes can step one orthogonal plot. Plants stay where planted. | Clearly distinguish paging, deploying, and moving. Show the exact reason a plot or card is unavailable. Give every spawn lane, tile, and sprite foot a reliable visual anchor. |
| Roster and tactics | Four human heroes and five plant companions. The game slices 17 gameplay sheets into 356 animation frames at startup. Plants link orthogonally, linked size boosts attack output, and three matching linked plants of the same tier can merge. | Show the real network edges, bonus, and merge outcome before committing. Make heroes earn their mobility and plants earn their permanence. |
| Threats and Core | Four enemy behaviors with four extra appearances. Night disables the active abilities of some defenders until the rechargeable visor is on. The Core starts at 20 HP, changes art across damage bands, and has decorative cables. A first nonfatal drop to 3 HP or below triggers Last Sprout for 4 Sun and a temporary enemy slow. | Read enemy roles across skins. Show how a breach in a side lane reached the Core. Separate Core cables from actual plant links. |
| Run structure | Plan starts at eight seconds outside the guide. Defend runs until scheduled bugs are resolved. Harvest offers three of five upgrade options and continues after a choice and a four-second minimum. Day ten and its multiples add a scaled Bolt Brute as Rust Titan. Seasons, night, breather budgets, and post-Day-30 Blights already exist. | Story completion, resumable checkpoints, and arena progression need implementation. Endless pacing needs recorded test runs. |
| Art and camera | The SampleScene references the Greenhouse Arena and wood-tray prefabs. One sunlit plate is wired. Soil shader, HUD art, a damageable Core, a URP day/night grade, and a small Cinemachine accent are already present. Four additional arena plates exist as concepts. | Produce reusable layers and anchor sheets. Integrate a second arena to validate the pipeline before commissioning more backdrops. |

**Three concrete issues to put at the top of the next pass**

1. **Plan warns about eligible lanes.** `MakeSchedule` marks lanes before the random spawn list is built, so an amber lane can receive zero bugs. Build the warning from the finished schedule. Show the first threat more strongly and distinguish it from later entries.
2. **The board tells two different stories about connections.** The Seed cables extend to the farthest plant in a lane. Actual link bonuses come from live orthogonal plant neighbors and can be disrupted by a Drill Mole. Show thick Core conduits as scenery and draw fine, short roots only between true neighbors. A broken link should have a visible gap and a restore cue.
3. **The source of Core damage can be hard to read.** Any enemy crossing the last lane threshold retires and applies damage to the center Core immediately. Give each lane an exit socket and trace the hit into the dome. Update the dome, HUD number, and breach cue from the same damage result. Preserve the current damage amounts in this pass.

## 2. Story — The Seed That Sang

Years after the city gardens went dry, the Eden-9 greenhouse still keeps one seed warm. On the morning its power starts to fail, four greenhouse defenders hear a note coming from inside the Seed Core. Pip, a tiny watering-can caretaker, recognizes it as a call to the city's irrigation relays. If the team wakes four gardens, the relays can carry the Seed's roots through the old water network.

Mother Rust built that network. When the water stopped flowing, she received an emergency instruction to clear anything growing in the pipes. Her Rust Bugs are still following the order. Each restored garden sends her a piece of the city she used to care for. By the time the team reaches her, the old caretaker can hear the Seed too.

| Hero | Who they are | How the story shows it |
| --- | --- | --- |
| **Dubu** | The patient protector who stands closest to trouble. | A shield catch buys the Seed time. Later Dubu leaves a secure position to protect a new opening. |
| **Bubu** | The curious tinkerer who likes fixing things that seem lost. | The frost rope catches a bug in the dark. At the relay, Bubu uses the same gear to recover a disconnected line. |
| **Arham** | The observant scout who notices what others miss. | Arham spots the first live relay signal and calls the lane the group must hold. |
| **Jojo** | A loud, inventive mechanic with a soft spot for battered machines. | The mortar clears a swarm. In the finale, Jojo recognizes Mother Rust's shutdown command and helps Pip change it. |

**Opening, delivered in two short beats**

> **Pip** — “The city went quiet. This seed is still singing.”
>
> **Dubu** — “Then let's keep it safe.”
>
> **Objective** — Stop the bugs before they reach the Seed Core.

In the finale, after the Titan falls, the player taps **Plant the relay seed**. A flower pushes through the grate. Mother Rust starts her old command, pauses, and says, “Garden detected. Watering.” The nearest dry pipe fills. Across the city map, four restored districts light up and a distant fifth sends a distress signal.

Tell the story through the map, the relay light, and changes to the environment. Save dialogue for calm beats between waves and keep it skippable. Use the four existing human hero designs as canon, with Pip as the guide and plant companions as expressive characters. Reserve a new illustration for the flower reveal after the opening slice lands.

## 3. Story and Endless share the same combat

**Story** is a first-pass proposal of four chapters with three authored waves each. Start each chapter with a curated roster, starting Sun, and a fresh Core. Keep the player's formation and Harvest rewards across the three waves of that chapter. Save completed relays and chapter entry locally so the player can leave between chapters. At 0 HP, offer **Retry chapter**. Restoring the fourth relay completes the story.

| Chapter | Waves | Player lesson and payoff |
| --- | --- | --- |
| **Dawn Nursery** | 1–3 | Use the guide or skip it. Hold the center, move a hero to a revealed side lane, then link plants to strengthen the formation. On clear, power the first relay and reveal a green nursery on the map. |
| **Moonlit Canopy** | 4–6 | Teach night with a single clear threat. Show who stays active before deployment. Let the player use Bubu, Jojo, Lotus Frost, or Sunflower in their existing roles and try the visor when daylight defenders fall quiet. Introduce the flying bypass after that lesson. Restore the canopy lamps. |
| **Rustworks Gate** | 7–9 | Teach the Drill Mole's bypass and link interruption. Give the player time to answer with a mobile hero and a second root route. Offer a same-tier trio merge when there is enough Sun and room to understand the trade. Reopen the main water valve. |
| **Seedheart Siege** | 10–12 | Combine familiar threats and preview the Titan's arrival lane. Hold the strongest formation through the finale, then plant the relay seed for the ending. Build a distinct Titan sprite only after the fight works with the current Brute stand-in. |

**Example of a teaching wave — Dawn Nursery, Wave 2**

- Show a center Beetle icon and a later side-lane arrow before starting. Highlight one reachable step for Dubu. Keep the player in control of when the first new movement lesson begins.
- Send a single Beetle down the center. Send a second Beetle down the announced side lane once the first engagement is underway. Show the travel path on selecting Dubu and accept any valid defense. A planted blocker on the side is also a good answer.
- At Harvest, briefly recognize what actually happened. For a successful move and block, “Dubu covered the side lane.” Preview one useful upgrade based on the state of the Core, with all three options still available.

**Endless** is offered after the first chapter. Players can also choose **Skip to Endless** from the start. Give it a separate run and record. Start from a fresh board. Keep defenders, Sun, upgrades, and Core HP between days. Save after the Harvest choice as the next planning state. Display **Days survived**, **Best Day**, and which garden was restored during the expedition. A resumed run restores the same upcoming wave and retains the player's selections. A failed run keeps its record and revealed map progress.

Treat late-game balance as a design pass. Existing wave counts cap at 52 regular spawns, regular HP scaling stops after its exponent reaches 100, and repeat damage and armor upgrades can keep compounding. Before changing those values, record clears, leaks, Sun, and upgrade choices at Days 1, 3, 10, 31, and a late checkpoint. Vary wave patterns and give each modifier a preview. Test replacements for repeated flat upgrades, such as a once-per-wave emergency shield or a link reward, after baseline measurements. Keep Story's authored progression and Endless's day formulas independent.

### New arena — Pumpyard Hustle

A rooftop rainwater switchyard beyond the restored city. Big teal cisterns and amber pressure gauges frame a quiet, dry clay center. The skyline is wider on iPad. Red and charcoal inlet gates signal the Rust approach. Filling a relay sends visible water through a pipe and raises a rooftop garden beyond the arena.

On marked Endless waves, an optional relay plot activates one orthogonal step from a useful defense position. The player can move a hero onto it and hold for **three seconds** to earn **2 Sun**, once that wave. Plants can still use that plot normally. Show a footprint, a three-segment meter, and the reward before the player moves. Reset the meter if the hero leaves before completion. If the player skips the relay, the normal wave still advances. Is opening a gap in the formation worth the extra Sun?

Prototype with the current arena and a simple relay marker. Advance to production art if players can explain the reward and make different choices depending on lane pressure. Introduce the event on a calm wave after players know how to move a hero.

## 4. Art direction and combat feel

Keep the warm wood and brass UI, blue greenhouse glass, lively green plant companions, and scarlet-and-charcoal machines from the reference. Give the battle floor a lower contrast than the frame. The strongest moving accents belong to a current threat, the player's action, and the Seed's response. Keep the expressive hero faces for portraits. In battle, preserve Dubu's short curls and broad blue shield, Bubu's blue hair streak and rope reel, Arham's dark hair and orange scarf, and Jojo's copper locs and shoulder mortar. Show defenders from the rear and bugs approaching face-first. [Approved hero concept](Art/Concepts/Bloombots_Human_Heroes_Concept_Banner.png)

### Environment and grid production

- Use the current greenhouse plate for the opening slice. Adapt **Moonlit Canopy** as the first new stage. Deliver a clean backplate, separately placeable foliage and lamps, a low-noise floor texture, a shared pipe and relay kit, and an anchor overlay for both portrait frames. Reuse the Core, wood tray, HUD, cards, and contact shadows.
- Fit all **40 plots and their relevant targets** in the gap between the upper HUD and tray. Side scenery can extend outside it. Keep the board steady when the visor appears. Use actual phone and tablet capture frames when locking the art.
- Record **five pipe mouths, the board boundaries, and the Core center** for each arena. Use the existing `ArenaBoard.World` / `TryCell` pair for the grid, actor feet, hit tests, root lines, and ground effects. The Rustworks concept paints three large gates and Seedheart paints one. Keep those as architecture and use five separate, aligned gameplay outlets.
- Keep the **plot shader** shallow and rounded, with quiet interior grain and stable neutral contrast. Add short directional chevrons for incoming lanes, a corner outline and check for a legal plot, an X for a blocked plot, and paired brackets for a hero move. Test the compressed far row before adding ornament.
- Use the existing **four Core damage stages** as milestones. Show the exact HP in the HUD and the damage delta on impact. At low HP, retain a steady crack or missing piece when flashes are disabled. Use flowing Core conduits as a health and restoration cue and show combat links as short roots between adjacent plants.

### Feedback brief

| Moment | What the player should read |
| --- | --- |
| **Deploy or move** | The rear-view sprite lifts from the card, projects onto a plot, and snaps feet-first on release. A brief soil puff and a wallet delta confirm a paid placement. A cancelled drag returns cleanly. Hero selection shows reachable neighbors and a short path with a destination marker. |
| **Guard and shoot** | Dubu braces before contact, his shield gives a short directed impact spark, and Arham's shot leaves the weapon on the release pose. A small directional recoil and quick target hit pose explain cause and effect. Give Cactus a distinct thorn-return cue. |
| **Bubu's rope** | The hook follows a visible travel path from the hand socket, tightens at contact, and signals the slow with a persistent frost marker. Use the actual target distance and tracking. Clear the rope promptly if the target is defeated. |
| **Frost, mortar, and healing** | Lotus sends a low wave along its lane. Mortars show an arc and ground ring covering their actual impact area. Sunflower uses a leaf and plus symbol on the defender it heals. Give each effect a distinct silhouette that survives the night grade. |
| **Enemy trick and breach** | Brute raises its clamps. Moth shows a lift and landing with a grounded shadow. Mole shows a short dirt trail and a broken-link symbol. Reuse these cues across skins. Lead a breach from its lane into the Core before the player's attention returns to the board. |
| **Merge, Harvest, and defeat** | Label the selected plant's chain size so the tray pips read as links. Outline the exact matching trio and destination before merging, then show the new tier and freed plots. At Harvest, let the board settle, show three icon-led reward cards with exact effects, and confirm the selected reward. On defeat, dim the arena around the one saved sprout and make retry easy to find. |

Prototype small contact accents around 80–120 ms and let combat timing continue through presentation. Reserve camera movement for safe beats such as Harvest and boss introductions. At zero motion and flash, impacts must still read through shape, timing, and labels. Keep the first polish pass understandable in silence. If sound comes back into scope, prioritize placement, shield contact, breach, and Harvest.

## 5. Code changes that enable the design

Make small extractions with behavior checks after each. Keep the current projection, authored wood UI, pooling, and stable card IDs as migration anchors.

| Order | Evidence in the current code | Proposed change and acceptance |
| --- | --- | --- |
| **1. Unify placement feedback** | `EndlessArena.Placement.cs` evaluates card eligibility and cell occupancy through several UI paths. Hover collapses several rejection cases into “PLOT OCCUPIED.” | Query one typed placement result for preview and tap. Revalidate on release and commit Sun, cooldown, and occupancy once. Cover occupied plots, enemy overlap, low Sun, cooldown, guide restrictions, and swipe cancellation. |
| **2. Extract authored encounters and the director** | `EndlessArena.cs` builds the wave, sets phase, handles rewards, and seeds a random generator from the clock. `EndlessArena.FieldGuide.cs` replaces the spawn list with integer unit indexes. | Introduce a wave specification with stable IDs, lane, spawn time, and enemy behavior and skin. Feed the guide, authored Story, and Endless through the same schedule consumer. Derive warnings from that schedule. Save deterministic wave and reward seeds for repeatable tuning and resume. |
| **3. Separate combat events from effects** | `EndlessArena.Combat.cs` resolves shots, tether, and mortar through arrival callbacks in `ArenaEffects.cs`. It also reads `PoseTime`, which advances in `ArenaActor.Draw`. | Put windup, recovery, travel, and hit resolution on the simulation clock. Send narrow presentation events for launch, hit, slow, and defeat. Preserve existing launch and impact timing, target lifetime checks for pooled actors, and cancellation behavior. Turning effects off must leave the same battle result. |
| **4. Make unit and arena art data explicit** | `UnitSpec.All`, `ArenaAssets.sheets`, and `ArenaFrames.json` depend on matching order. `GreenhouseArena.prefab` binds one arena while `ArenaLook.cs` creates camera and volume settings at runtime. `ArenaHud.cs` builds the dialog hierarchy in code. | Add an ID-keyed unit art catalog and validation for card IDs, frames, pivots, and sockets. Define one stage asset for layers, anchors, palette, and a prefab-backed stage view. Move editable camera, lighting, and dialog hierarchy into authored assets as each feature is touched. Keep the runtime mesh and transient FX where dynamic behavior warrants them. |
| **5. Add versioned checkpoints and improve hot paths from measurements** | `PlayerPrefs` stores settings, guide completion, and Best Day. Per-frame board legality, actor scans, HUD formatting, and Core cable updates are candidates for profiling. | Save only at safe phase boundaries. Persist mode, next wave, seed, Core HP, Sun, units by plot, upgrades, and remaining cooldowns. Write atomically and retain a previous valid checkpoint. Profile in a full late wave before caching occupancy, batching UI updates, or changing effect pools. |

## 6. Delivery order and acceptance

| Milestone | What ships | Gate |
| --- | --- | --- |
| **A. Baseline and first-minute polish** | Capture the current opening and repair schedule markers, placement feedback, and breach readability. Polish the existing guided wave and its first Harvest. | Guided, skipped, and replayed starts work. Upward drag places exactly once, horizontal swipe reliably pages, and invalid or cancelled input spends zero Sun. A new player can point to the Seed and tell what caused the last hit. |
| **B. Dawn Nursery vertical slice** | Three authored waves, one hero move lesson, one link lesson, readable reward cards, a first relay restore, and a menu return. | In a small first-time playtest, aim for at least four of five players to place the guard and shooter and explain the Core objective without help. At least one later wave should prompt a useful hero move and a deliberate plant link. |
| **C. Campaign and second arena** | Arena definition, Moonlit integration, night lesson, then Rustworks and Seedheart, chapter retry and resume, and the ending. | Chapter load, retry, completion, and resume each have predictable results. The visor shows active and sleeping defenders clearly. Every stage aligns its five outlets, 40 plots, and Core on phone and tablet. |
| **D. Endless and Hustle** | Resume after Harvest, expedition map progress, seeded wave patterns, Blight previews, and the Pumpyard prototype. | A saved run produces the same upcoming schedule after restart. Verify days 3, 10, 31, and a late wave with a full board. Playtest whether the relay creates a real choice before commissioning the full Pumpyard art. |

Before calling the slice polished, verify a small notched iPhone, a tall iPhone, and a portrait iPad on real touch input. Include a pause during a drag and during a projectile, app background and foreground, all HUD and tray hit areas, a side-lane breach, a merge, and night with motion and flash at zero. Capture a busy wave and a sustained session with frame time and memory. Set the release performance target against the chosen devices and measured results.

For the first production ticket, give each of the first three waves one readable choice, then reward the player with the first district turning green. Defer extra units, deck randomization, daily challenges, and a full meta-economy until the opening slice demonstrates that the existing roster and grid are fun.

### Project references

- [Main Menu scene](Assets/Scenes/Main%20Menu.unity), [SampleScene](Assets/Scenes/SampleScene.unity), [Greenhouse Arena prefab](Assets/Prefabs/GreenhouseArena.prefab), [wood tray prefab](Assets/UI/BottomBanner/BottomBannerCanvas.prefab)
- [Wave and run flow](Assets/Gameplay/EndlessArena.cs), [combat](Assets/Gameplay/EndlessArena.Combat.cs), [placement and links](Assets/Gameplay/EndlessArena.Placement.cs), [Field Guide](Assets/Gameplay/EndlessArena.FieldGuide.cs)
- [Projection](Assets/Gameplay/ArenaBoard.cs), [plot shader](Assets/Gameplay/PerspectivePlots.shader), [Core damage art binding](Assets/Gameplay/SeedCoreView.cs), [camera and lighting](Assets/Gameplay/ArenaLook.cs)
- [Dawn Nursery](Assets/Productionn/07_Arena_Dawn_Nursery.png), [Moonlit Canopy](Assets/Productionn/08_Arena_Moonlit_Canopy.png), [Rustworks Gate](Assets/Productionn/09_Arena_Rustworks_Gate.png), [Seedheart Siege](Assets/Productionn/10_Arena_Seedheart_Siege.png)
- [Earlier mechanics handoff](ANTIGRAVITY_GAMEPLAY_HANDOFF.md), [earlier art and first-minute notes](Assets/Productionn/Art_Direction_and_First_Minute.md)
