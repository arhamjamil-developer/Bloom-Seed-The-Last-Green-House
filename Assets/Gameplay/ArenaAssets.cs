using System;
using UnityEngine;

namespace Bloombots.Gameplay
{
    public sealed class ArenaAssets : ScriptableObject
    {
        public Texture2D backdrop;
        public Texture2D props;
        public Texture2D[] sheets;
        public TextAsset frames;
        public Sprite rustTitan;
        public Shader effectsShader;
        public Material unitMaterial;
        public Material artMaterial;
        public ArenaHeaderView header;
        public TMPro.TMP_FontAsset uiLabels, uiBody;
        public Texture2D productionCast;
        [Serializable]
        public sealed class Atlas
        {
            public Sheet[] sheets;
            public Frame[] props;
        }

        [Serializable]
        public sealed class Sheet
        {
            public Frame[] frames;
            public float idleHeight;
        }

        [Serializable]
        public sealed class Frame
        {
            public float x, y, width, height, pivotX, pivotY;

            public Sprite Build(Texture2D texture)
            {
                int left = Mathf.RoundToInt(x * texture.width);
                int bottom = Mathf.RoundToInt(y * texture.height);
                int right = Mathf.RoundToInt((x + width) * texture.width);
                int top = Mathf.RoundToInt((y + height) * texture.height);
                return Sprite.Create(texture, new Rect(left, bottom, right - left, top - bottom),
                    new Vector2(pivotX, pivotY), 100, 0, SpriteMeshType.FullRect);
            }
        }
    }
}
