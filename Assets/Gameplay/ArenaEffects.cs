using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bloombots.Gameplay
{
    internal sealed class ArenaEffects : IDisposable
    {
        private sealed class Effect
        {
            public SpriteRenderer Sprite;
            public LineRenderer Line;
            public Vector3 From, To;
            public Func<Vector3> Follow, Origin;
            public Action Arrive;
            public Color Color;
            public float Time, Travel, Duration, Arc, Radius;
            public bool Rope, Trace, Hit;
        }

        private readonly Transform root;
        private readonly Material lineMaterial, ropeMaterial, artMaterial;
        private readonly List<Effect> active = new();
        private readonly Stack<Effect> idle = new();

        public ArenaEffects(Transform root, ArenaAssets assets)
        {
            this.root = root;
            lineMaterial = new Material(assets.effectsShader);
            ropeMaterial = new Material(assets.effectsShader);
            ropeMaterial.SetFloat("_Weave", 1);
            artMaterial = assets.artMaterial;
        }

        public void Fly(Sprite sprite, Vector3 from, Vector3 to, Func<Vector3> follow,
            float seconds, float arc, float size, Action arrive)
        {
            var effect = Rent();
            effect.From = from;
            effect.To = to;
            effect.Follow = follow;
            effect.Travel = effect.Duration = Mathf.Max(.04f, seconds);
            effect.Arc = arc;
            effect.Arrive = arrive;
            effect.Sprite.sprite = sprite;
            effect.Sprite.transform.position = from;
            effect.Sprite.transform.localScale = Vector3.one * (size / sprite.bounds.size.y);
            effect.Sprite.enabled = true;
        }

        public void Tether(Func<Vector3> origin, Func<Vector3> follow, float travel, Action arrive)
        {
            var effect = Rent();
            effect.Rope = true;
            effect.Origin = origin;
            effect.From = origin();
            effect.To = follow();
            effect.Follow = follow;
            effect.Travel = travel;
            effect.Duration = travel + .38f;
            effect.Arrive = arrive;
            effect.Color = new Color(.31f, .92f, 1);
            effect.Line.positionCount = 12;
            effect.Line.sharedMaterial = ropeMaterial;
            effect.Line.widthMultiplier = .041f;
            effect.Line.enabled = true;
            for (int i = 0; i < effect.Line.positionCount; i++)
                effect.Line.SetPosition(i, effect.From);
        }

        public void Ring(Vector3 origin, Color color, float radius, float duration = .38f)
        {
            var effect = Rent();
            effect.From = origin;
            effect.Color = color;
            effect.Radius = radius;
            effect.Travel = effect.Duration = duration;
            effect.Line.positionCount = 25;
            effect.Line.sharedMaterial = lineMaterial;
            effect.Line.widthMultiplier = .043f;
            effect.Line.enabled = true;
            for (int i = 0; i < effect.Line.positionCount; i++)
                effect.Line.SetPosition(i, origin);
        }

        public void Breach(Vector3 origin, Vector3 core, float flash)
        {
            var effect = Rent();
            effect.Trace = true;
            effect.From = origin;
            effect.To = core;
            effect.Color = new Color(1, .53f, .23f, Mathf.Lerp(.55f, .95f, flash));
            effect.Travel = .30f;
            effect.Duration = .42f;
            effect.Line.positionCount = 2;
            effect.Line.sharedMaterial = lineMaterial;
            effect.Line.widthMultiplier = .057f;
            effect.Line.SetPosition(0, origin);
            effect.Line.SetPosition(1, origin);
            effect.Line.enabled = true;
        }

        public void Tick(float dt, bool resolveHits = true)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var effect = active[i];
                effect.Time += dt;
                float t = Mathf.Clamp01(effect.Time / effect.Travel);
                if (effect.Follow != null)
                    effect.To = effect.Follow();
                if (effect.Sprite.enabled)
                {
                    Vector3 p = Vector3.Lerp(effect.From, effect.To, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * effect.Arc;
                    effect.Sprite.transform.position = p;
                }
                else if (effect.Rope)
                {
                    effect.From = effect.Origin();
                    float reach = t * (1 - Mathf.Clamp01((effect.Time - effect.Travel - .17f) / .21f));
                    Vector3 end = Vector3.Lerp(effect.From, effect.To, reach);
                    Vector3 direction = end - effect.From;
                    Vector3 normal = new Vector3(-direction.y, direction.x, 0).normalized;
                    for (int k = 0; k < effect.Line.positionCount; k++)
                    {
                        float u = k / (float)(effect.Line.positionCount - 1);
                        Vector3 p = Vector3.Lerp(effect.From, end, u);
                        p += normal * (Mathf.Sin(u * Mathf.PI) * Mathf.Sin(u * 15 - effect.Time * 16) * .075f * (1 - t * .7f));
                        effect.Line.SetPosition(k, p);
                    }
                    effect.Line.startColor = effect.Line.endColor = effect.Color;
                }
                else if (effect.Trace)
                {
                    effect.Line.SetPosition(0, Vector3.Lerp(effect.From, effect.To, Mathf.Clamp01((t - .21f) / .79f)));
                    effect.Line.SetPosition(1, Vector3.Lerp(effect.From, effect.To, t));
                    effect.Line.startColor = new Color(effect.Color.r, effect.Color.g, effect.Color.b, 0);
                    effect.Line.endColor = effect.Color;
                }
                else
                {
                    float radius = Mathf.Lerp(.08f, effect.Radius, t);
                    for (int k = 0; k < effect.Line.positionCount; k++)
                    {
                        float a = k * Mathf.PI * 2 / (effect.Line.positionCount - 1);
                        effect.Line.SetPosition(k, effect.From + new Vector3(Mathf.Cos(a), Mathf.Sin(a) * .58f, 0) * radius);
                    }
                    Color color = effect.Color;
                    color.a *= 1 - t;
                    effect.Line.startColor = effect.Line.endColor = color;
                }

                if (!effect.Hit && t >= 1)
                {
                    effect.Hit = true;
                    if (resolveHits)
                        effect.Arrive?.Invoke();
                }
                if (effect.Time >= effect.Duration)
                {
                    active.RemoveAt(i);
                    Release(effect);
                }
            }
        }

        public void Clear()
        {
            foreach (var effect in active)
                Release(effect);
            active.Clear();
        }

        private Effect Rent()
        {
            if (!idle.TryPop(out var effect))
            {
                var go = new GameObject("Arena projectile", typeof(SpriteRenderer));
                go.transform.SetParent(root, false);
                var line = new GameObject("Arena trail", typeof(LineRenderer));
                line.transform.SetParent(root, false);
                effect = new Effect { Sprite = go.GetComponent<SpriteRenderer>(), Line = line.GetComponent<LineRenderer>() };
                effect.Sprite.sharedMaterial = artMaterial;
                effect.Sprite.sortingOrder = 245;
                effect.Line.sortingOrder = 246;
                effect.Line.numCapVertices = 3;
            }
            effect.Time = 0;
            effect.Hit = effect.Rope = effect.Trace = false;
            effect.Sprite.enabled = effect.Line.enabled = false;
            active.Add(effect);
            return effect;
        }

        private void Release(Effect effect)
        {
            effect.Sprite.enabled = effect.Line.enabled = false;
            effect.Follow = null;
            effect.Origin = null;
            effect.Arrive = null;
            idle.Push(effect);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(lineMaterial);
            UnityEngine.Object.Destroy(ropeMaterial);
        }
    }
}
