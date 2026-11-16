# Bloom Heroes · Garden menu and storyboard

The menu follows the layout of `Final V.png` with a large title above a central mascot, raised side buttons, a record plaque, and a gold Play button at the bottom. The greenhouse, cactus, leaf motifs, and story illustrations belong to Bloom Heroes.

[Cast and loading layout](Art/Concepts/Bloom_Cast_and_Loading_Layout.png) · [Storyboard sheet](Art/Concepts/The_Seed_That_Sang_Storyboard.png) · [Bloom Lover type proof](Art/Concepts/Bloom_Lover_Type_Proof.png)

## Menu flow

| Button | Opens |
| --- | --- |
| **Play / Story** | The opening story, with **Next**, **Skip Intro**, and a separate back arrow. The last beat starts Dawn Nursery. |
| **Endless** | The existing endless arena and its own Best Day record. |
| **Map** | A four-garden chapter preview. Dawn gains a **Restored** badge after the third wave. |
| **Cards / Field Guide / Settings** | The existing deck, tutorial, and accessibility settings. |

The menu uses separate portrait backplates and independently placed uGUI buttons. The short button labels are baked into individual images with Barlow Condensed and Fredoka. Story text stays live and editable in the scene. The downloaded Bloom Lover is shown in a local type proof because the author marks that download for personal use. [Bloom Lover author’s license note](https://www.dafont.com/bloom-lover.font)

## The Seed That Sang

The opening is player-paced. The suggested seconds describe an edit for a trailer or animated version. In the game, each page stays up until the player advances or skips.

| Suggested time or trigger | Art | Beat |
| --- | --- | --- |
| 0–3 s | Eden-9 over the dark rooftops | The city gardens have gone quiet. One greenhouse still has power. |
| 3–7 s | Pip and the Seed Core | Pip hears the Seed calling to four sleeping gardens. |
| 7–11 s | Dubu and Arham facing the Rust Bugs | Protect the Core. **Start Dawn Nursery** takes the player to the arena. |
| Wave 1 | Live board | Hold the center lane. The Field Guide can teach Dubu's placement and Arham's supporting shot. |
| Wave 2 | Live board | A second lane opens. Select a hero and step onto a highlighted adjacent plot, or defend it with a plant. |
| Wave 3 | Live board | Protect three lanes. Nearby plants form a visible root network. |
| After Wave 3 | First Garden banner | The first relay wakes. The player can open the garden map or return to the menu. |

Dawn Nursery is the playable Story chapter. Moonlit Canopy, Rustworks Gate, and Seedheart Siege are available as illustrated previews. Their encounters, chapter checkpoints, and the Mother Rust finale are described in the [design and polish plan](BLOOM_HEROES_GAME_DESIGN_AND_POLISH.md).

## Arena visuals

- Planning chevrons come from scheduled spawns. The first lane's arrow is stronger.
- The perspective soil shader draws a check or X for placement, brackets for hero movement, and gold-green roots only across adjacent plants that share a live connection.
- A short trace runs from a breached lane toward the Core. The HUD reports the lane and actual HP lost. Existing damage stages still show the state of the Seed.

## Art sources and prompts

The loading and cast update uses the built-in image generator. [Garden Cast](Assets/UI/MainMenu/Garden/Garden_Cast.png), [iPhone stage](Assets/UI/MainMenu/Garden/GardenStage_iPhone.png), [iPad stage](Assets/UI/MainMenu/Garden/GardenStage_iPad.png), [Seed Core loading art](Assets/UI/MainMenu/Loading/SeedCore_Loading.png)

### Garden cast

Generate a transparent game UI cast cutout, landscape near 3:2 aspect ratio. Reference one is the approved lineup with exactly four stylized HUMAN Bloom Heroes. Match all their faces, clothing, relative body builds, hair, skin tones and equipment. In a single compact full-body group portrait standing shoulder to shoulder in a shallow V, Dubu with cropped black curls and broad blue shield at left, Bubu with black bob and cobalt hair streak and rope reel at middle-left, Arham with dark swept hair, orange scarf and seed blaster at middle-right, Jojo with auburn locs and shoulder mortar at right. Include the small adorable lime green Cactus Guard with big eyes and blue leaf shield centered in the foreground at their feet, from reference two. Behind the team and around them entirely transparent. No ground, no island, no text, no background, no visible rectangle. Keep all hair, gear, legs and feet inside frame. Compact cohesive silhouette with shared golden greenhouse lighting. Kid-friendly polished hand-painted 3D adventure game illustration, clear silhouettes and strong color separation. The final will overlay the central island in a mobile game menu.

### Seed Core loading art

Design one richly colored portrait 9:16 loading screen background illustration for Bloom Heroes To Save Seed. In the same solarpunk greenhouse game art style as the supplied reference, paint a close shot of a single green sprout glowing in a luminous glass Seed Core in the central middle, warm brass base, winding teal irrigation pipes, sunlight into bright green foliage, cobalt blue arched glass ceiling above. Shape of Seed Core directly inspired by the game reference. Keep the top 25 percent calm and dark blue for an existing overlaid title, and bottom 26 percent dark emerald with low detail for an overlaid loading bar and white status text. Central seed and dome should fit within the middle two-thirds width even if cropped at tall iPhone. Candy-bright gold green cyan highlights, polished stylized adventure game depth. Background only. No text, no logo, no progress bar, no interface, no characters.

### Phone and tablet garden stage edits

Precise object removal edit. Remove just the little green cactus hero with blue cape and shield from the center of this image, and remove its identifiable cactus silhouette in the reflection. Inpaint its space with matching greenhouse pond water and distant skyline. Restore the stone surface and small contact shadows of the EMPTY central mossy stone island so a separate character sprite can stand on it. Preserve exactly the same perspective, open sky, framing, aspect ratio, palette, landscape, greenhouse glass, flowering island shape and position, foreground pond and edge composition. No characters, no UI, no text. This is a clean game menu backplate.

### Original garden menu

The built-in image generator produced the original raster artwork. The two generated sheets were sliced into separate PNGs. Exact button labels were typeset afterward from project fonts.

### ButtonKit

Use case ui-mockup. Generate a production-ready transparent PNG sprite sheet for a child-friendly mobile tower defense game called Bloom Heroes. Input Final V is a reference for the chunky 3D depth and proportions of its graphite utility button and big gold PLAY button. Use very similar geometry, silhouette, material lighting and depth, adapting the decoration to the original game's living greenhouse setting. Exactly SIX isolated objects, each centered in one cell of a regular 3-column 2-row grid with wide fully transparent gutters, enough margin so no overlaps, landscape sheet. Top row left a chunky square dark graphite basalt and inset walnut button tile topped by a 3D embossed shield and seed packets. Top center identical chunky square tile topped by an open storybook with green leaf page. Top right identical tile topped by a gold trophy with tiny green leaf. Bottom left identical tile topped by a folded parchment map and amber compass. Bottom center identical tile topped by a bright golden question mark and small rolled field guide. Bottom right identical tile topped by a silver grey gear. Every icon occupies upper 65% of its same size tile, the lower 26% has visibly blank very dark label bed reserved for exact labels added later. Rich top-left bevel highlights, beautiful colorful hand-painted 3D game UI, warm gold bottom edging and understated brass fasteners. True transparency, no canvas background, no grid marks, no lettering or words, no cast shadow extending into gutters.

### PlayButton

Use case ui-mockup. Generate one isolated 3D mobile game PLAY button body on true transparent background, closely matching the oversized gold Play button in Final V reference for shape, raised honey-yellow top surface, beveled amber lower extrusion and graphite stone rim, softly clipped corners, lush small leaf sprigs only on upper left and upper right. Very wide horizontal silhouette approximately 3:1 width to height and centered in the canvas. Premium hand-painted clear materials. A dark brown simple play triangle is embossed on the left within the gold face. Leave the central and right gold face deliberately blank with quiet, readable texture to receive the exact word PLAY from the project font. No letters, no numbers, no banner surrounding the button, no other elements.

### OpeningStoryboard

Use case illustration-story. A cinematic 2x2 storyboard of FOUR separate 16:9 landscape game story panels for Bloom Heroes: The Seed That Sang. Use supplied Bloom Heroes concept hero image as character identity reference. A clean rectangular 2 by 2 contact sheet, four equal fully painted borderless panels meeting edge to edge, with exact middle divides and no lines or shared scenery. Overall wide 16:9, richly colored storybook rendering, expressive stylized human adventurers, soft hand-painted 3D lighting, warm greens, cobalt, brass, and crimson menace. Panel top left: silent dry rooftop city at predawn, glass Eden-9 greenhouse on a roof lit by one warm amber window, distant irrigation pipes. Panel top right: close shot of a tiny expressive brass and teal watering-can caretaker Pip peering at one luminous green sprout inside a glass Seed Core, the light pulsing like a tiny music note made of motes. Panel bottom left: inside the greenhouse, curly-haired Dubu raising broad blue shield next to dark tousled Arham in orange scarf with seed blaster, red charcoal rust beetles visible in distant far gate. Panel bottom right: after defense the first garden relay glows green, teal irrigation water rushes through brass pipe and new foliage blooms with golden sunlight; a moment of relief and joy. Keep focal elements near each panel's center and important detail out of the outer edges. No letters, logos, text, panel numbers, or dialog bubbles.

### MenuGarden

Use case stylized-concept. Generate a portrait mobile main menu BACKGROUND PLATE for Bloom Heroes To Save Seed, 9:19 tall framing. Final V reference is a composition guide: immersive game environment, quiet blue upper third for overlaid logo, clear side corridors in the middle for icon tiles, one cute prominent mascot on a little foreground island lower center, lower fifth left free for a large overlay Play button. Set the original environment inside a solarpunk glass greenhouse high above a quiet city: round glass ribbed roof, cobalt blue sky visible upper half, peaceful teal irrigation basin, gardens, brass pipes on sides, honey-colored stones. Put one adorable glossy bright green Cactus Guard plant companion with large eyes and a blue/gold shield on a small mossy stone island centered at x50%, y65% down, clearly visible at phone size. Gentle reflection in water. Lush rounded stylized hand-painted 3D game visuals, strong golden highlights, actual depth, blue/teal water with restrained stylized caustics. Keep outer sides quiet where UI will overlay. Background art only, no text, no interface, no buttons, no logos. In a portrait crop keep the entire mascot and stage centered.

### MenuTablet

Use case precise-object-edit / UI background. Adapt this existing Bloom Heroes portrait main-menu plate into a 3:4 portrait TABLET composition. Keep the very same smiling green Cactus Guard, blue cape, gold-rimmed leaf shield, front stone island, blue glass roof, skyline, brass pipes, cobalt and teal palette and polished 3D cartoon art. Widen the greenhouse and pond left and right, compose the island to x50% and y62% from top, shrink its width to 65 percent of tablet width. Blue quiet open sky in upper 30 percent for title. Leave room for UI tiles at sides and large gold button in lower 14 percent. Preserve complete cactus silhouette. Full bleed. No text, overlays, logos, or buttons.

