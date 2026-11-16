using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bloombots.Gameplay
{
    internal sealed class ArenaLook
    {
        private readonly Camera camera;
        private readonly CinemachineCamera wide, focus;
        private readonly VolumeProfile profile;
        private readonly ColorAdjustments color;
        private readonly Vignette vignette;
        private readonly Light2D sun;
        private float pulse, grade;
        private Color gardenTint = Color.white;

        public void SetGarden(int garden)
        {
            gardenTint = garden switch
            {
                1 => new Color(.77f, .92f, 1f),
                2 => new Color(1f, .83f, .68f),
                3 => new Color(.91f, .81f, 1f),
                _ => Color.white
            };
        }

        public ArenaLook(Transform parent, Camera camera)
        {
            this.camera = camera;
            camera.orthographic = true;
            camera.allowHDR = true;
            camera.orthographicSize = ArenaBoard.Height * .5f;
            var brain = camera.GetComponent<CinemachineBrain>() ?? camera.gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, .55f);
            wide = MakeCamera(parent, "Board framing", 10);
            focus = MakeCamera(parent, "Wave and harvest accent", 0);

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.volumeLayerMask = data.volumeLayerMask | 1;
            var obj = new GameObject("Day and night grade", typeof(Volume));
            obj.transform.SetParent(parent, false);
            var volume = obj.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 30;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = .92f;
            bloom.intensity.value = .24f;
            bloom.scatter.value = .58f;
            color = profile.Add<ColorAdjustments>(true);
            vignette = profile.Add<Vignette>(true);
            vignette.color.value = new Color(.07f, .14f, .22f);
            vignette.smoothness.value = .46f;

            foreach (var light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (light.lightType != Light2D.LightType.Global)
                    continue;
                sun = light;
                break;
            }
            if (sun == null)
            {
                sun = new GameObject("Arena sun", typeof(Light2D)).GetComponent<Light2D>();
                sun.transform.SetParent(parent, false);
                sun.lightType = Light2D.LightType.Global;
            }
        }

        public void Accent() => pulse = 1.15f;

        public float Tick(float dt, bool night, bool visor, float motion)
        {
            pulse = Mathf.Max(0, pulse - dt);
            // Preserve a minimum world width for the full five-lane board on narrow phones.
            float size = ArenaBoard.WideCameraSize(camera.aspect);
            SetLens(wide, size);
            SetLens(focus, size * Mathf.Lerp(1f, .975f, motion));
            focus.Priority = pulse > 0 && motion > .01f ? 20 : 0;

            float target = night ? (visor ? .44f : 1f) : 0;
            grade = Mathf.MoveTowards(grade, target, dt * 1.9f);
            color.postExposure.value = Mathf.Lerp(.02f, -.79f, grade);
            color.contrast.value = Mathf.Lerp(5, 13, grade);
            color.saturation.value = Mathf.Lerp(6, -15, grade);
            color.colorFilter.value = gardenTint * Color.Lerp(Color.white,
                visor ? new Color(.73f, 1, .77f) : new Color(.61f, .76f, 1), grade);
            vignette.intensity.value = Mathf.Lerp(.13f, .28f, grade);
            sun.color = Color.Lerp(new Color(1, .97f, .90f), new Color(.59f, .76f, 1), grade);
            sun.intensity = Mathf.Lerp(1.05f, .61f, grade);
            return grade;
        }

        public void Dispose() => Object.Destroy(profile);

        private CinemachineCamera MakeCamera(Transform parent, string name, int priority)
        {
            var view = new GameObject(name, typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            view.transform.SetParent(parent, false);
            view.transform.position = new Vector3(0, 0, -10);
            view.transform.rotation = Quaternion.identity;
            view.Priority = priority;
            SetLens(view, ArenaBoard.Height * .5f);
            return view;
        }

        private static void SetLens(CinemachineCamera view, float size)
        {
            var lens = view.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = size;
            view.Lens = lens;
        }
    }
}
