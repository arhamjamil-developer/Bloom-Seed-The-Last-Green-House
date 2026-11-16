using UnityEngine;

namespace Bloombots.Gameplay
{
    public sealed partial class EndlessArena
    {
        private Vector2 dragScreen;
        private bool PlayingBoard => !paused && phase is (Phase.Plan or Phase.Defend) && (!GuideBlocksWave || GuidedPlacement);

        private void Arm(BloomCardView view)
        {
            if (PlayingBoard && !dragging && cardMap.TryGetValue(view, out var card) && CanUse(card))
            {
                armed = armed == card ? null : card;
                selected = null;
            }
        }

        internal bool BeginCardDrag(BloomCardView view, int pointer, Vector2 screen)
        {
            if (!PlayingBoard || dragging || !cardMap.TryGetValue(view, out var card) || !CanUse(card))
                return false;

            armed = card;
            selected = null;
            dragging = true;
            dragPointer = pointer;
            MoveCardDrag(pointer, screen);
            return true;
        }

        internal void MoveCardDrag(int pointer, Vector2 screen)
        {
            if (!dragging || dragPointer != pointer)
                return;
            dragScreen = screen;
            RefreshDragPreview();
        }

        private void RefreshDragPreview()
        {
            hoverValid = board.TryCell(dragScreen, out hover);
            int kind = armed.Kind;
            var sprite = frames[kind][0];
            float row = hoverValid ? hover.y + .5f : ArenaBoard.Rows;
            float pixelsPerUnit = arenaCamera.pixelHeight / (2 * arenaCamera.orthographicSize);
            float scale = pixelsPerUnit * HeightFor(UnitSpec.All[kind], false) / idleHeights[kind] * ArenaBoard.DepthScale(row);
            Vector2 size = (Vector2)sprite.bounds.size * scale;
            if (hoverValid)
            {
                bool legal = CanUse(armed) && CanDeployAt(hover);
                Vector2 tile = arenaCamera.WorldToScreenPoint(board.World(hover.x + .5f, row));
                hud.ShowPlacement(sprite, tile, size, legal ? $"DEPLOY  ·  {armed.View.SunCost} SUN" : GuidedPlacement ? "USE THE GLOWING PLOT" : "PLOT OCCUPIED",
                    legal ? new Color(.12f, .43f, .23f, .98f) : new Color(.62f, .17f, .12f, .98f));
            }
            else
            {
                size *= Mathf.Max(1, Screen.width * .21f / size.y);
                Vector2 lifted = dragScreen + Vector2.up * (Screen.width * .05f);
                float sidePadding = Mathf.Max(size.x * .45f, Screen.width * .11f);
                lifted.x = Mathf.Clamp(lifted.x, sidePadding, Screen.width - sidePadding);
                lifted.y = Mathf.Min(lifted.y, Screen.height * .94f - size.y * .8f);
                hud.ShowPlacement(sprite, lifted, size, "DRAG TO A PLOT", new Color(.29f, .16f, .075f, .97f));
            }
        }

        internal void EndCardDrag(int pointer, Vector2 screen)
        {
            if (!dragging || dragPointer != pointer)
                return;
            if (board.TryCell(screen, out var cell))
                Deploy(cell);
            ClearSelection();
        }

        internal void CancelCardDrag(int pointer)
        {
            if (dragging && dragPointer == pointer)
                ClearSelection();
        }

        internal void BoardTap(Vector2 screen)
        {
            if (!PlayingBoard || dragging)
                return;
            if (!board.TryCell(screen, out var cell))
            {
                ClearSelection();
                selected = null;
                return;
            }
            if (armed != null)
            {
                if (Deploy(cell))
                    ClearSelection();
                else
                    Say(GuidedPlacement ? "Use the glowing plot." : "Choose an empty plot.");
                return;
            }

            if (GuideBlocksWave)
                return;

            var occupant = units[cell.x, cell.y];
            if (occupant != null)
            {
                selected = selected == occupant ? null : occupant;
                if (selected != null)
                    Say(selected.Spec.Hero ? "Tap an adjacent open plot to move." : "Select a linked trio to merge.");
            }
            else if (selected != null && selected.Alive && selected.Spec.Hero)
                MoveHero(selected, cell);
            else
                selected = null;
        }

        private void MoveHero(ArenaActor hero, Vector2Int destination)
        {
            int distance = Mathf.Abs(hero.Cell.x - destination.x) + Mathf.Abs(hero.Cell.y - destination.y);
            if (hero.MoveRemaining > 0 || distance != 1 || !CanOccupy(destination))
            {
                Say("Heroes move one open tile at a time.");
                return;
            }

            var previous = hero.Cell;
            units[previous.x, previous.y] = null;
            units[destination.x, destination.y] = hero;
            hero.MoveFrom = new Vector2(hero.Lane, hero.Row);
            hero.Cell = destination;
            hero.MoveRemaining = .28f;
            hero.Target = null;
            hero.Windup = 0;
            hero.Play(previous.x == destination.x ? Pose.Move : Pose.Side);
            hero.Renderer.flipX = destination.x < previous.x;
        }

        private bool CanUse(CardSlot card) => card != null && GuideAllowsCard(card)
            && sun >= card.View.SunCost && clock >= card.ReadyAt;

        private bool CanDeployAt(Vector2Int cell) => GuideAllowsPlot(cell) && CanOccupy(cell);

        private bool CanOccupy(Vector2Int cell)
        {
            if (units[cell.x, cell.y] != null)
                return false;
            foreach (var actor in actors)
                if (actor.Alive && actor.Spec.Enemy && Mathf.RoundToInt(actor.Lane) == cell.x
                    && actor.Row >= cell.y && actor.Row < cell.y + 1)
                    return false;
            return true;
        }

        private bool Deploy(Vector2Int cell)
        {
            if (!PlayingBoard || !CanUse(armed) || !CanDeployAt(cell))
                return false;

            var slot = armed;
            var actor = Rent(slot.Kind, cell.x, cell.y + .5f, false);
            actor.Cell = cell;
            units[cell.x, cell.y] = actor;
            sun -= slot.View.SunCost;
            banner.ShowSunSpend(slot.View.SunCost);
            BloomFeedback.Planted();
            slot.ReadyAt = clock + slot.View.RechargeSeconds * recharge;
            chainDirty = true;
            Burst(board.World(cell.x + .5f, cell.y + .5f), new Color(.62f, 1, .38f), .39f);
            OnGuideDeployment();
            return true;
        }

        private void ClearSelection()
        {
            armed = null;
            dragging = hoverValid = false;
            hud?.HidePlacement();
        }

        private bool Linkable(int lane, int row)
        {
            var actor = units[lane, row];
            return actor != null && actor.Alive && !actor.Spec.Hero && clock >= brokenUntil[lane, row];
        }

        private void RebuildChains()
        {
            if (!chainDirty && clock < nextLinkChange)
                return;

            chainDirty = false;
            nextLinkChange = float.PositiveInfinity;
            System.Array.Clear(links, 0, links.Length);
            System.Array.Clear(visited, 0, visited.Length);
            for (int row = 0; row < ArenaBoard.Rows; row++)
            for (int lane = 0; lane < ArenaBoard.Lanes; lane++)
            {
                if (brokenUntil[lane, row] > clock)
                    nextLinkChange = Mathf.Min(nextLinkChange, brokenUntil[lane, row]);
                if (visited[lane, row] || !Linkable(lane, row))
                    continue;
                WalkChain(new Vector2Int(lane, row));
                foreach (var cell in component)
                    links[cell.x, cell.y] = component.Count;
            }
        }

        private void WalkChain(Vector2Int start)
        {
            component.Clear();
            if (!Linkable(start.x, start.y))
                return;
            component.Add(start);
            visited[start.x, start.y] = true;
            for (int i = 0; i < component.Count; i++)
            {
                var cell = component[i];
                Visit(cell.x - 1, cell.y);
                Visit(cell.x + 1, cell.y);
                Visit(cell.x, cell.y - 1);
                Visit(cell.x, cell.y + 1);
            }
        }

        private void Visit(int lane, int row)
        {
            if (!ArenaBoard.Contains(lane, row) || visited[lane, row] || !Linkable(lane, row))
                return;
            visited[lane, row] = true;
            component.Add(new Vector2Int(lane, row));
        }

        private void MergeSelected()
        {
            if (!PlayingBoard || dragging)
                return;
            if (selected == null || !selected.Alive || selected.Spec.Hero || selected.Tier >= 3)
            {
                Say("Tap one of three matching linked plants first.");
                return;
            }

            System.Array.Clear(visited, 0, visited.Length);
            WalkChain(selected.Cell);
            ArenaActor second = null, third = null;
            foreach (var cell in component)
            {
                var candidate = units[cell.x, cell.y];
                if (candidate == selected || candidate.Kind != selected.Kind || candidate.Tier != selected.Tier)
                    continue;
                if (second == null)
                    second = candidate;
                else
                {
                    third = candidate;
                    break;
                }
            }
            if (third == null)
            {
                Say("Link three matching plants of the same tier.");
                return;
            }

            Defeat(second);
            Defeat(third);
            selected.Tier++;
            selected.Scale *= 1.09f;
            selected.MaxHp *= 1.45f;
            selected.Hp = selected.MaxHp;
            chainDirty = true;
            Burst(board.World(selected.Lane + .5f, selected.Row), new Color(1, .84f, .24f), .70f);
            Say("Bloom tier " + (int)selected.Tier + "!");
            look.Accent();
        }

        private void PaintBoard(float grade)
        {
            RebuildChains();
            for (int row = 0; row < ArenaBoard.Rows; row++)
            for (int lane = 0; lane < ArenaBoard.Lanes; lane++)
            {
                var cell = new Vector2Int(lane, row);
                bool threat = phase == Phase.Plan && incoming[lane];
                Color32 color = threat ? IncomingPlot : EmptyPlot;
                PlotMark mark = threat && row % 2 == 0
                    ? lane == firstLane ? PlotMark.FirstThreat : PlotMark.Incoming
                    : PlotMark.None;
                if (links[lane, row] >= 2)
                    color = LinkedPlot;
                if (brokenUntil[lane, row] > clock)
                {
                    color = BlockedPlot;
                    mark = PlotMark.Blocked;
                }
                if (selected != null && selected.Alive && selected.Cell == cell)
                {
                    color = SelectedPlot;
                    mark = PlotMark.Step;
                }
                else if (selected != null && selected.Alive && selected.Spec.Hero && selected.MoveRemaining <= 0
                    && Mathf.Abs(selected.Cell.x - lane) + Mathf.Abs(selected.Cell.y - row) == 1 && CanOccupy(cell))
                {
                    color = SelectedPlot;
                    mark = PlotMark.Step;
                }
                else if (armed != null)
                {
                    bool legal = CanUse(armed) && CanDeployAt(cell);
                    color = legal ? LegalPlot : BlockedPlot;
                    mark = legal ? PlotMark.Legal : PlotMark.Blocked;
                }
                if (GuidedPlacement)
                {
                    color = cell == GuidePlot ? AimPlot : EmptyPlot;
                    mark = cell == GuidePlot ? PlotMark.Legal : PlotMark.None;
                }
                if (dragging && hoverValid && hover == cell)
                {
                    bool legal = CanUse(armed) && CanDeployAt(cell);
                    color = legal ? AimPlot : BlockedPlot;
                    mark = legal ? PlotMark.Legal : PlotMark.Blocked;
                }
                if (IsTitanDanger(cell))
                {
                    color = TitanDanger;
                    mark = PlotMark.Blocked;
                }
                Vector4 roots = links[lane, row] >= 2 ? new Vector4(
                    RootEdge(lane - 1, row), RootEdge(lane + 1, row),
                    RootEdge(lane, row - 1), RootEdge(lane, row + 1)) : Vector4.zero;
                board.Paint(lane, row, color, mark, roots);
            }
            board.Draw(grade);
        }

        private float RootEdge(int lane, int row) => ArenaBoard.Contains(lane, row) && links[lane, row] >= 2 ? 1 : 0;

        private void Say(string message)
        {
            notice = message;
            noticeUntil = clock + 4;
        }

        private void RefreshHud()
        {
            bool legal = false;
            if (PlayingBoard)
            {
                for (int y = 0; y < ArenaBoard.Rows && !legal; y++)
                for (int x = 0; x < ArenaBoard.Lanes && !legal; x++)
                    legal = CanDeployAt(new Vector2Int(x, y));
            }
            foreach (var slot in cards)
            {
                bool guideLocked = !GuideAllowsCard(slot);
                bool cardLegal = legal && !guideLocked;
                float remaining = Mathf.Max(0, slot.ReadyAt - clock);
                remaining = Mathf.Ceil(remaining * 20) / 20;
                if (slot.LastSun == sun && slot.LastLegal == cardLegal && slot.LastGuideLocked == guideLocked &&
                    Mathf.Approximately(slot.LastRemaining, remaining))
                    continue;
                slot.View.SetAvailability(sun, remaining, cardLegal, guideLocked);
                slot.LastSun = sun;
                slot.LastRemaining = remaining;
                slot.LastLegal = cardLegal;
                slot.LastGuideLocked = guideLocked;
            }

            int pips = selected != null && selected.Alive ? links[selected.Cell.x, selected.Cell.y] : 0;
            if (displayedSun != sun || displayedPips != pips)
            {
                banner.SetDisplay(sun, pips);
                displayedSun = sun;
                displayedPips = pips;
            }
            string season = Season switch { 0 => "SPRING", 1 => "SUMMER", 2 => "AUTUMN", _ => "WINTER" };
            string status = paused ? "PAUSED" : GuideBlocksWave ? "FIELD GUIDE" : phase switch
            {
                Phase.Plan => $"WAVE IN {Mathf.Max(0, Mathf.CeilToInt(PlanSeconds - phaseTime))}",
                Phase.Defend when rustTitan != null => $"TITAN {Mathf.CeilToInt(rustTitan.Fuse)}",
                Phase.Defend => $"{spawns.Count - cursor + ActiveEnemyCount()} BUGS LEFT",
                Phase.Harvest => "HARVEST",
                Phase.StoryCleared => "GARDEN RESTORED",
                _ => "RUN COMPLETE"
            };
            string tip = GuideBlocksWave ? "" : rustTitan != null ? "Blast incoming! Move heroes off the red plots."
                : armed != null ? $"Costs {armed.View.SunCost} Sun. Choose a glowing plot." : clock < noticeUntil ? notice
                : phase == Phase.Plan && dayNumber == 1 ? "Protect the Seed Core. Deploy defenders in the amber lane."
                : storyGarden == 0 && phase == Phase.Plan && dayNumber == 2 ? "A second lane opens. Tap a hero, then an adjacent blue plot."
                : storyGarden == 0 && phase == Phase.Plan && dayNumber == 3 ? "Place plants side by side to grow stronger roots."
                : storyGarden == 1 && phase == Phase.Plan ? "Night-ready defenders can fight without the visor."
                : storyGarden == 2 && phase == Phase.Plan ? "Cover the outer lanes. Burrowers can break plots."
                : storyGarden == 3 && dayNumber == GardenWaveCount && phase == Phase.Plan ? "Rust Titan lands after the swarm. Spread out your defenders."
                : phase == Phase.Plan && dayNumber == 20 && phaseTime < 4 ? "Arham: The seed sent a shoot through the city wall."
                : phase == Phase.Plan && dayNumber == 30 && phaseTime < 4 ? "Jojo: We can grow a new city if the seed makes it to dawn."
                : phase == Phase.Plan && IsTitanWave ? "Rust Titan lands after the swarm. Spread out your defenders."
                : phase == Phase.Plan && dayNumber == 31 && phaseTime < 3 ? "Pip: Mother Rust has gone quiet. New nests are waking."
                : night && !visorOn && (blight == Blight.None || phaseTime % 6 < 3) ? "Some defenders sleep. Use the visor."
                : blight == Blight.QuickSwarm ? "QUICK SWARM  ·  Bugs move faster."
                : blight == Blight.IronShells ? "IRON SHELLS  ·  Bugs have more health."
                : blight == Blight.SunDrought ? "SUN DROUGHT  ·  Sun gathers slowly."
                : phase == Phase.Plan ? "Amber plots show the incoming lanes."
                : "Drag cards onto plots. Swipe across cards to browse.";
            string day = storyRun ? $"WAVE {dayNumber}/{GardenWaveCount}" : $"DAY {dayNumber}  ·  {season}";
            hud.Refresh(day, status, $"CORE  {coreHp}/{CoreMax}", tip, sun, night, visorOn, charge);
        }
    }
}
