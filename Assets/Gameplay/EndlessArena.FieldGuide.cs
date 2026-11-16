using UnityEngine;

namespace Bloombots.Gameplay
{
    public sealed partial class EndlessArena
    {
        private enum GuideStep { None, Intro, Dubu, Arham, Ready, Fight }

        private const string GuideSeenKey = "Bloombots.FieldGuideSeen";
        private static readonly Vector2Int GuardPlot = new(2, 4);
        private static readonly Vector2Int ShooterPlot = new(2, 5);
        [SerializeField] private FieldGuideView fieldGuidePrefab;
        private FieldGuideView fieldGuide;
        private CardHandPager guideHand;
        private GuideStep guideStep;
        private bool requestedGuide;

        private bool GuideBlocksWave => guideStep is GuideStep.Intro or GuideStep.Dubu or GuideStep.Arham or GuideStep.Ready;
        private bool GuidedPlacement => guideStep is GuideStep.Dubu or GuideStep.Arham;
        private Vector2Int GuidePlot => guideStep == GuideStep.Dubu ? GuardPlot : ShooterPlot;

        private void PrepareGuide()
        {
            requestedGuide = MainMenuController.TakeTutorialRequest();
            if (fieldGuidePrefab == null)
                return;

            guideHand = banner.GetComponentInChildren<CardHandPager>(true);
            fieldGuide = Instantiate(fieldGuidePrefab, hud.GuideParent, false);
            fieldGuide.Bind(AdvanceGuide, SkipGuide);
            fieldGuide.gameObject.SetActive(false);
        }

        private void StartGuideIfNeeded()
        {
            bool wanted = requestedGuide || storyGarden <= 0 && PlayerPrefs.GetInt(GuideSeenKey, 0) == 0;
            requestedGuide = false;
            SetGuideStep(GuideStep.None);
            if (!wanted || fieldGuide == null)
                return;

            var guard = cards.Find(slot => slot.View.CardId == "dubu");
            var shooter = cards.Find(slot => slot.View.CardId == "arham");
            if (guard == null || shooter == null || guard.View.SunCost + shooter.View.SunCost > sun)
                return;

            SetGuideStep(GuideStep.Intro);
        }

        private bool GuideAllowsCard(CardSlot card) => guideStep switch
        {
            GuideStep.Intro or GuideStep.Ready => false,
            GuideStep.Dubu => card.View.CardId == "dubu",
            GuideStep.Arham => card.View.CardId == "arham",
            _ => true
        };

        private bool GuideAllowsPlot(Vector2Int cell) => !GuidedPlacement || cell == GuidePlot;

        private void AdvanceGuide()
        {
            if (paused || phase != Phase.Plan)
                return;
            if (guideStep == GuideStep.Intro)
                SetGuideStep(GuideStep.Dubu);
            else if (guideStep == GuideStep.Ready)
                StartGuideWave();
        }

        private void OnGuideDeployment()
        {
            if (guideStep == GuideStep.Dubu)
                SetGuideStep(GuideStep.Arham);
            else if (guideStep == GuideStep.Arham)
                SetGuideStep(GuideStep.Ready);
        }

        private void SkipGuide()
        {
            if (paused || !GuideBlocksWave)
                return;
            ClearSelection();
            CompleteGuide();
            phaseTime = 0;
            Say("Protect the Seed Core. Deploy defenders in the amber lane.");
        }

        private void StartGuideWave()
        {
            ClearSelection();
            SetGuideStep(GuideStep.Fight);
            spawns.Clear();
            cursor = 0;
            spawns.Add(new Spawn { Lane = 2, Kind = 9, At = .7f });
            spawns.Add(new Spawn { Lane = 2, Kind = 9, At = 4.8f });
            spawns.Add(new Spawn { Lane = 2, Kind = 9, At = 5.5f });
            spawns.Add(new Spawn { Lane = 2, Kind = 9, At = 7f });
            spawns.Add(new Spawn { Lane = 2, Kind = 10, At = 7.2f });
            MarkIncoming();
            phase = Phase.Defend;
            phaseTime = 0;
            Say("Arham fires automatically. Dubu blocks bugs that get close.");
        }

        private void CompleteGuide()
        {
            SetGuideStep(GuideStep.None);
            PlayerPrefs.SetInt(GuideSeenKey, 1);
            PlayerPrefs.Save();
        }

        private void SetGuideStep(GuideStep step)
        {
            guideStep = step;
            guideHand?.LockToHeroes(GuideBlocksWave);
            RefreshGuideView();
        }

        private void RefreshGuideView()
        {
            if (fieldGuide == null)
                return;
            if (paused || !GuideBlocksWave)
            {
                fieldGuide.gameObject.SetActive(false);
                return;
            }

            switch (guideStep)
            {
                case GuideStep.Intro:
                    fieldGuide.Show(1, false, "Protect the Seed!", "Bugs march in from the top. Keep them away from the Seed Core.", "Next");
                    break;
                case GuideStep.Dubu:
                    fieldGuide.Show(1, false, "Put Dubu on guard", "Drag Dubu to the glowing plot. You can also tap his card, then tap the plot.");
                    break;
                case GuideStep.Arham:
                    fieldGuide.Show(2, true, "Back up Dubu", "Place Arham on the glowing plot behind Dubu. He shoots up the lane.");
                    break;
                case GuideStep.Ready:
                    fieldGuide.Show(3, true, "Hold the lane!", "Five bugs are coming. Watch Arham shoot while Dubu holds the line.", "Start Wave");
                    break;
            }
        }
    }
}
