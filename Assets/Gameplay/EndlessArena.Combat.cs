using UnityEngine;

namespace Bloombots.Gameplay
{
    public sealed partial class EndlessArena
    {
        private static float HeightFor(UnitSpec spec, bool boss) =>
            boss ? 1.42f : spec.Hero ? .78f : spec.Role == Role.Brute ? .67f : spec.Enemy ? .53f : .59f;

        private ArenaActor Rent(int kind, float lane, float row, bool boss)
        {
            if (!actorPool.TryPop(out var actor))
                actor = new ArenaActor(transform, assets.unitMaterial, assets.artMaterial, pixel, shadow);
            var spec = UnitSpec.All[kind];
            actor.Spec = spec;
            actor.Kind = kind;
            actor.Serial = ++serial;
            actor.Lane = lane;
            actor.Row = row;
            actor.Boss = boss;
            actor.Alive = true;
            actor.UsedTrick = false;
            actor.Tier = 1;
            actor.Hp = actor.MaxHp = spec.Hp * (spec.Enemy ? EnemyHpMultiplier() : armor) * (boss ? 3.2f : 1);
            actor.Target = null;
            actor.Cell = new Vector2Int(-1, -1);
            actor.Windup = actor.Slow = actor.MoveRemaining = actor.Retire = actor.PoseTime = 0;
            actor.AttackIn = .1f;
            actor.Pose = Pose.Idle;
            actor.Fuse = 0;
            actor.Renderer.sprite = boss && assets.rustTitan != null ? assets.rustTitan : frames[kind][0];
            actor.BaseHeight = boss ? actor.Renderer.sprite.bounds.size.y : idleHeights[kind];
            actor.Scale = HeightFor(spec, boss) / actor.BaseHeight;
            actor.Renderer.flipX = false;
            actor.Renderer.color = Color.white;
            actor.Renderer.transform.position = board.World(lane + .5f, row);
            actor.Renderer.transform.localScale = Vector3.one * (actor.Scale * ArenaBoard.DepthScale(row));
            actor.Renderer.gameObject.name = boss ? "Rust Titan" : spec.Id;
            actor.Renderer.gameObject.SetActive(true);
            actors.Add(actor);
            return actor;
        }

        private float EnemyHpMultiplier() => Mathf.Pow(1.06f, Mathf.Min(dayNumber - 1, 100))
            * (Season == 3 ? 1.18f : 1) * (blight == Blight.IronShells ? 1.32f : 1);

        private void Release(ArenaActor actor)
        {
            actor.Alive = false;
            actor.Target = null;
            actor.Renderer.gameObject.SetActive(false);
            actorPool.Push(actor);
        }

        private void TickUnits(float dt)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (!actor.Alive)
                    continue;
                if (actor.Boss)
                {
                    TickRustTitan(actor, dt);
                    continue;
                }
                actor.AttackIn -= dt;
                actor.Slow = Mathf.Max(0, actor.Slow - dt);
                if (actor.MoveRemaining > 0)
                {
                    MoveActor(actor, dt);
                    continue;
                }

                if (actor.Windup > 0)
                {
                    actor.Windup -= dt;
                    if (actor.Windup <= 0)
                    {
                        var target = actor.Target;
                        actor.Target = null;
                        if (target != null && target.Alive && target.Serial == actor.TargetSerial
                            && (actor.Spec.Enemy || CanAct(actor)))
                            Attack(actor, target);
                    }
                    continue;
                }
                if (actor.Pose == Pose.Attack && actor.PoseTime < AttackSeconds
                    || actor.Pose == Pose.Hurt && actor.PoseTime < .21f)
                    continue;

                if (actor.Spec.Enemy)
                    TickEnemy(actor, dt);
                else
                    TickDefender(actor);
                if (phase == Phase.Lost)
                    return;
            }
        }

        private void MoveActor(ArenaActor actor, float dt)
        {
            actor.MoveRemaining = Mathf.Max(0, actor.MoveRemaining - dt);
            float t = 1 - actor.MoveRemaining / .28f;
            actor.Lane = Mathf.Lerp(actor.MoveFrom.x, actor.Cell.x, t);
            actor.Row = Mathf.Lerp(actor.MoveFrom.y, actor.Cell.y + .5f, t);
            if (actor.MoveRemaining <= 0)
            {
                actor.Renderer.flipX = false;
                actor.Play(Pose.Idle);
            }
        }

        private void TickDefender(ArenaActor actor)
        {
            actor.Play(Pose.Idle);
            if (!CanAct(actor) || actor.AttackIn > 0)
                return;
            ArenaActor target = PickTarget(actor);
            if (target != null)
                Windup(actor, target);
        }

        private bool CanAct(ArenaActor actor) => !night || visorOn || actor.Spec.NightReady;

        private ArenaActor PickTarget(ArenaActor from)
        {
            ArenaActor pick = null;
            float best = float.PositiveInfinity;
            foreach (var to in actors)
            {
                if (!to.Alive || to == from)
                    continue;
                float dx = Mathf.Abs(to.Lane - from.Lane);
                float dy = from.Row - to.Row;
                if (from.Spec.Role == Role.Heal)
                {
                    float gap = dx + Mathf.Abs(dy);
                    float priority = to.Hp / to.MaxHp;
                    if (!to.Spec.Enemy && priority < .99f && gap <= from.Spec.Range && priority < best)
                    {
                        pick = to;
                        best = priority;
                    }
                    continue;
                }
                if (!to.Spec.Enemy || (dy < -.35f && !to.Boss))
                    continue;
                float crossLane = from.Spec.Role is Role.Mortar or Role.Tether ? 1.15f : .38f;
                float distance = dx + Mathf.Abs(dy);
                if (dx > crossLane || distance > from.Spec.Range || distance >= best)
                    continue;
                pick = to;
                best = distance;
            }
            return pick;
        }

        private void TickEnemy(ArenaActor enemy, float dt)
        {
            int lane = Mathf.RoundToInt(enemy.Lane);
            ArenaActor blocker = null;
            float gap = float.PositiveInfinity;
            for (int row = 0; row < ArenaBoard.Rows; row++)
            {
                var candidate = units[lane, row];
                if (candidate == null || !candidate.Alive || Mathf.Abs(candidate.Lane - lane) > .45f)
                    continue;
                float distance = candidate.Row - enemy.Row;
                if (distance < -.28f || distance >= gap)
                    continue;
                gap = distance;
                blocker = candidate;
            }

            if (blocker != null && gap <= enemy.Spec.Range)
            {
                if (!enemy.UsedTrick && enemy.Spec.Role is (Role.Moth or Role.Mole))
                {
                    enemy.UsedTrick = true;
                    if (enemy.Spec.Role == Role.Mole)
                    {
                        brokenUntil[blocker.Cell.x, blocker.Cell.y] = clock + 7;
                        chainDirty = true;
                        Burst(GroundPoint(blocker), new Color(.80f, .46f, .21f), .47f);
                    }
                    enemy.Row = blocker.Row + .56f;
                    enemy.Play(Pose.Move);
                    return;
                }
                if (enemy.AttackIn <= 0)
                    Windup(enemy, blocker);
                else
                    enemy.Play(Pose.Idle);
                return;
            }

            float speed = enemy.Spec.Role switch
            {
                Role.Brute => .88f, Role.Moth => 1.75f, Role.Mole => 1.28f, _ => 1.28f
            };
            speed *= Mathf.Min(1 + .01f * dayNumber, 1.5f);
            if (blight == Blight.QuickSwarm || Season == 1)
                speed *= blight == Blight.QuickSwarm ? 1.23f : 1.08f;
            if (enemy.Slow > 0)
                speed *= .50f;
            enemy.Row += speed * dt;
            enemy.Play(Pose.Move);
            if (enemy.Row < CoreRow)
                return;

            int damage = enemy.Spec.Role == Role.Brute ? 3 : 2;
            Vector3 dome = core.transform.position + Vector3.up * .24f;
            effects.Breach(board.World(lane + .5f, ArenaBoard.Rows), dome, flash);
            Defeat(enemy);
            int lost = Mathf.Min(coreHp, damage);
            SetCoreHealth(coreHp - damage, hit: true);
            Say($"Lane {lane + 1} breached. Seed Core lost {lost} HP.");
            Burst(dome, new Color(1, .33f, .16f), .57f);
            if (coreHp == 0)
            {
                phase = Phase.Lost;
                phaseTime = 0;
                RecordBest(dayNumber - 1);
                ClearSelection();
                selected = null;
            }
            else if (coreHp <= 3 && !lastSprout)
            {
                lastSprout = true;
                sun += 4;
                foreach (var actor in actors)
                    if (actor.Alive && actor.Spec.Enemy)
                        actor.Slow = Mathf.Max(actor.Slow, 4);
                Say("Last Sprout! Pip grants 4 Sun and slows the swarm.");
            }
        }

        private static void Windup(ArenaActor actor, ArenaActor target)
        {
            actor.Target = target;
            actor.TargetSerial = target.Serial;
            actor.Windup = AttackCue;
            actor.AttackIn = actor.Spec.Cooldown;
            actor.Play(Pose.Attack);
        }

        private void Attack(ArenaActor source, ArenaActor target)
        {
            var role = source.Spec.Role;
            int targetSerial = target.Serial;
            float damage = source.Spec.Damage;
            if (source.Spec.Enemy)
            {
                damage *= 1 + Mathf.Min(dayNumber, 130) * .036f;
                if (Mathf.Abs(target.Row - source.Row) > source.Spec.Range + .3f
                    || Mathf.Abs(target.Lane - source.Lane) > .5f)
                    return;
                Hurt(target, damage);
                if (target.Alive && target.Spec.Role == Role.Guard && !target.Spec.Hero && CanAct(target))
                    Hurt(source, 5 * target.Tier);
                if (role == Role.Brute)
                    Burst(GroundPoint(target), new Color(1, .53f, .17f), .45f);
                return;
            }

            int linked = source.Spec.Hero ? 1 : Mathf.Max(1, links[source.Cell.x, source.Cell.y]);
            damage *= attackPower * (1 + .10f * (linked - 1)) * (1 + .65f * (source.Tier - 1));
            Vector3 from = GroundPoint(source) + new Vector3(.07f, .41f, 0) * ArenaBoard.DepthScale(source.Row);
            Vector3 to = TargetPoint(target);
            switch (role)
            {
                case Role.Tether:
                    Vector3 last = to;
                    float travel = Mathf.Clamp(Vector3.Distance(from, to) / 6.8f, .10f, .52f);
                    int sourceSerial = source.Serial;
                    effects.Tether(() =>
                    {
                        if (Matches(source, sourceSerial))
                            from = GroundPoint(source) + new Vector3(.07f, .41f, 0) * ArenaBoard.DepthScale(source.Row);
                        return from;
                    }, () => CurrentTarget(target, targetSerial, ref last), travel, () =>
                    {
                        if (!Matches(source, sourceSerial) || !Matches(target, targetSerial))
                            return;
                        target.Slow = Mathf.Max(target.Slow, 3.6f);
                        Hurt(target, damage);
                        Burst(GroundPoint(target), new Color(.30f, .87f, 1), .28f);
                    });
                    break;
                case Role.Shot:
                    Vector3 lastShotPosition = to;
                    float seconds = Mathf.Clamp(Vector3.Distance(from, to) / 8.5f, .10f, .51f);
                    effects.Fly(props[2], from, to, () => CurrentTarget(target, targetSerial, ref lastShotPosition),
                        seconds, 0, .12f, () =>
                    {
                        if (Matches(target, targetSerial))
                            Hurt(target, damage);
                    });
                    break;
                case Role.Mortar:
                    int impactLane = Mathf.RoundToInt(target.Lane);
                    int impactRow = Mathf.FloorToInt(target.Row);
                    Vector3 landing = board.World(impactLane + .5f, impactRow + .5f);
                    effects.Fly(props[3], from, landing, null, .65f, .88f, .17f, () =>
                    {
                        foreach (var enemy in actors)
                        {
                            if (enemy.Alive && enemy.Spec.Enemy && Mathf.Abs(enemy.Lane - impactLane) <= 1.1f
                                && Mathf.Abs(Mathf.FloorToInt(enemy.Row) - impactRow) <= 1)
                                Hurt(enemy, damage);
                        }
                        Burst(landing, new Color(1, .55f, .15f), .66f, .53f);
                    });
                    break;
                case Role.Frost:
                    foreach (var enemy in actors)
                    {
                        if (!enemy.Alive || !enemy.Spec.Enemy || Mathf.Abs(enemy.Lane - source.Lane) > .38f
                            || (!enemy.Boss && enemy.Row > source.Row + .2f)
                            || Mathf.Abs(source.Row - enemy.Row) > source.Spec.Range)
                            continue;
                        enemy.Slow = Mathf.Max(enemy.Slow, 3.1f);
                        Hurt(enemy, damage);
                        Burst(GroundPoint(enemy), new Color(.39f, .83f, 1), .23f);
                    }
                    break;
                case Role.Heal:
                    target.Hp = Mathf.Min(target.MaxHp, target.Hp + damage);
                    Burst(GroundPoint(target), new Color(.53f, 1, .40f), .34f);
                    break;
                default:
                    if (Mathf.Abs(target.Row - source.Row) <= source.Spec.Range + .25f)
                    {
                        Hurt(target, damage);
                        Burst(GroundPoint(target), new Color(1, .70f, .28f), .34f);
                    }
                    break;
            }
        }

        private static bool Matches(ArenaActor actor, int lifetime) => actor.Alive && actor.Serial == lifetime;

        private Vector3 CurrentTarget(ArenaActor actor, int lifetime, ref Vector3 last)
        {
            if (Matches(actor, lifetime))
                last = TargetPoint(actor);
            return last;
        }

        private Vector3 GroundPoint(ArenaActor actor) => board.World(actor.Lane + .5f, actor.Row);
        private Vector3 TargetPoint(ArenaActor actor) => GroundPoint(actor) + Vector3.up * .29f;

        private void Hurt(ArenaActor actor, float damage)
        {
            if (!actor.Alive)
                return;

            actor.Hp -= damage;
            if (actor.Hp > 0)
            {
                if (actor.Pose != Pose.Attack && actor.MoveRemaining <= 0)
                    actor.Play(Pose.Hurt);
                Burst(GroundPoint(actor) + Vector3.up * .25f, new Color(1, .80f, .45f), .16f, .18f);
                return;
            }

            Defeat(actor);
            if (!actor.Spec.Enemy)
                return;
            sun += actor.Boss ? 8 : 1;
            Vector3 ground = GroundPoint(actor);
            Vector2 destination = sunTarget != null ? RectTransformUtility.WorldToScreenPoint(null, sunTarget.position)
                : new Vector2(Screen.width * .12f, Screen.height * .17f);
            var ray = arenaCamera.ScreenPointToRay(destination);
            Vector3 end = new Plane(Vector3.forward, Vector3.zero).Raycast(ray, out float depth)
                ? ray.GetPoint(depth) : ground;
            effects.Fly(props[1], ground + Vector3.up * .20f, end, null, .45f, .40f, .16f, null);
        }

        private void Defeat(ArenaActor actor)
        {
            if (actor == rustTitan)
            {
                rustTitan = null;
                titanAftermathUntil = clock + 1.15f;
            }
            actor.Alive = false;
            actor.Target = null;
            actor.Windup = 0;
            actor.Retire = .72f;
            actor.Play(Pose.Defeat);
            if (!actor.Spec.Enemy)
            {
                var cell = actor.Cell;
                if (ArenaBoard.Contains(cell.x, cell.y) && units[cell.x, cell.y] == actor)
                    units[cell.x, cell.y] = null;
                chainDirty = true;
                if (selected == actor)
                    selected = null;
            }
        }

        private void DrawActors(float dt)
        {
            for (int i = actors.Count - 1; i >= 0; i--)
            {
                var actor = actors[i];
                if (!actor.Alive)
                {
                    actor.Retire -= dt;
                    if (actor.Retire <= 0)
                    {
                        actors.RemoveAt(i);
                        Release(actor);
                        continue;
                    }
                }

                bool sleeping = actor.Alive && !actor.Spec.Enemy && !CanAct(actor);
                Color tint = sleeping ? new Color(.58f, .65f, .79f) : actor.Boss
                    ? Color.Lerp(Color.white, new Color(1, .43f, .22f), Mathf.Clamp01(1 - actor.Fuse / TitanFuse))
                    : actor.Tier > 1 ? new Color(1, .92f, .61f) : Color.white;
                if (!actor.Alive)
                    tint.a = Mathf.Min(1, actor.Retire * 5);
                actor.Renderer.color = tint;
                bool guarding = actor.Alive && !sleeping && !actor.Spec.Hero && actor.Spec.Role == Role.Guard;
                float lift = actor.Boss && actor.Alive
                    ? 1.15f * motion * Mathf.Clamp01((actor.Fuse - (TitanFuse - .22f)) / .22f)
                    : 0;
                actor.Draw(board, frames[actor.Kind], dt, guarding, AttackSeconds, lift);
            }
        }

        private void Burst(Vector3 center, Color color, float radius, float duration = .38f)
        {
            if (flash <= 0)
                return;
            color.a *= flash;
            effects.Ring(center, color, radius, duration);
        }
    }
}
