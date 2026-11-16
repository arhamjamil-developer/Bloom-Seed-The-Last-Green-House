# Bloom Heroes To Save Seed

One surviving seed can regrow the city. Dubu, Bubu, Arham, and Jojo defend the greenhouse with their plant companions as the rust bugs push in from above.

## Art direction

The key art keeps the heroes’ signature equipment, hair, and color cues. Cobalt blue belongs to glass and hero equipment. Leaf green marks life and placement. Amber marks the next attack lane. Red and charcoal mark the bugs and their machinery. Brass and dark wood carry the card tray’s materials into the menu.

The iPhone covers, menus, and tutorial export at 1080 × 2340. The iPad layouts export at 1536 × 2048. Treat the menu labels and buttons as layout references when building the Unity screen. Keep live text and hit areas inside the device safe area.

The built-in image generator produced the key illustration and four arena plates. The key-art brief called for the four established heroes and their signature equipment, the glowing Seed Core, Cactus Guard, Lotus Frost, and scarlet mechanical beetles inside the glasshouse. The four arena prompts shared a centered greenhouse camera and open floor, with sunlit irrigation, a cobalt moonlit canopy, red service machinery, and a shattered siege atrium as their distinct landmarks. The tutorial diagrams use the project’s actual card art, gameplay sprites, and Seed Core.

## Why the first wave is hard to read

SampleScene opens on the hero hand with 12 Sun and starts an eight-second planning timer. The first four seconds of the opening hint are story. The amber center column shows where the bugs will arrive, but it does not show where to place a blocker versus a shooter. The player then has to infer that shooting is automatic and that horizontal swipes reveal the plants.

## First-minute design

| Beat | What the player sees and does |
| --- | --- |
| Goal | Briefly light up the Seed Core and show a downward arrow from the center pipe. Say “Stop the bugs before they reach the Seed Core.” |
| Guard | Hold the first countdown. Highlight Dubu’s card and one center-lane plot. Advance when Dubu is placed there by drag or tap. |
| Shooter | Highlight Arham’s card and the plot one step closer to the Seed. Show a short shot preview pointing up the lane. Offer **Start Wave** after both placements. |
| First attack | Let a single Scrap Beetle enter before the rest of the wave. Keep its path and the shot visible. Once it is defeated, show a Sun gain at the wallet. |
| Plants and reward | Point to the Plants tab during a calm beat. Show Cactus Guard and Pea Pulser as familiar block and shoot roles. After the wave, explain that Harvest grants one upgrade. |

Keep **How to Play** available from the menu. A **Best Day** line can reuse the existing best-day record. During the first tutorial, introduce Sun cost at the first deployment and save merge and visor lessons for the first time each mechanic is relevant.

## Arena progression

| Arena plate | Design use |
| --- | --- |
| Dawn Nursery | Teach the center lane, blocking, and automatic fire. Keep the daytime floor bright so deployment cues read cleanly. |
| Moonlit Canopy | Introduce the visor and night-ready defenders. Use a short lesson when the first daylight-only defender stops attacking. |
| Rustworks Gate | Teach multi-lane pressure and Drill Mole disruptions. Pair each threatened lane with an amber pipe cue and mark interrupted plant links. |
| Seedheart Siege | Bring the Rust Titan and the damaged Seed Core into focus. Make the approach visible before the boss enters and use the encounter as a milestone in the endless run. |

Each plate has an open central floor for the existing projected grid. For a tall iPhone, center-crop the scenery and fit the board from the actual HUD and tray bounds. On iPad, use the wider side scenery without moving the lane centers away from the visible spawn cues. Confirm those alignments in Game View when integrating a plate.
