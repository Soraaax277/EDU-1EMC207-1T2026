using UnityEngine;
using UnityEngine.UI;

namespace Midterms
{
    public class HeistUI : MonoBehaviour
    {
        private GameState CurrentState = GameState.Infiltration;
        private GUIStyle TitleStyle;
        private GUIStyle SubtitleStyle;
        private GUIStyle ButtonStyle;

        [Header("Hierarchy Canvas UI (Optional)")]
        [SerializeField] private Text HierarchyObjectiveText;
        [SerializeField] private Text HierarchySubtitleText;
        [SerializeField] private GameObject HierarchyReticleObject;
        [SerializeField] private Text HierarchyReticleText;
        [SerializeField] private GameObject HierarchyVictoryPanel;
        [SerializeField] private Text HierarchyVictoryTitle;
        [SerializeField] private Text HierarchyVictorySubtitle;
        [SerializeField] private Button HierarchyRetryButton;
        [SerializeField] private Button HierarchyNextButton;
        [SerializeField] private GameObject HierarchyCaughtPanel;
        [SerializeField] private Text HierarchyCaughtTitle;
        [SerializeField] private Text HierarchyCaughtSubtitle;

        public void AssignHierarchyUI(
            Text objText,
            Text subText,
            GameObject reticleObj,
            Text reticleText,
            GameObject victoryPanel = null,
            Text victoryTitle = null,
            Text victorySub = null,
            Button retryBtn = null,
            Button nextBtn = null,
            GameObject caughtPanel = null,
            Text caughtTitle = null,
            Text caughtSub = null)
        {
            HierarchyObjectiveText = objText;
            HierarchySubtitleText = subText;
            HierarchyReticleObject = reticleObj;
            HierarchyReticleText = reticleText;
            HierarchyVictoryPanel = victoryPanel;
            HierarchyVictoryTitle = victoryTitle;
            HierarchyVictorySubtitle = victorySub;
            HierarchyRetryButton = retryBtn;
            HierarchyNextButton = nextBtn;
            HierarchyCaughtPanel = caughtPanel;
            HierarchyCaughtTitle = caughtTitle;
            HierarchyCaughtSubtitle = caughtSub;
        }

        public void ClearHierarchyUI()
        {
            HierarchyObjectiveText = null;
            HierarchySubtitleText = null;
            HierarchyReticleObject = null;
            HierarchyReticleText = null;
            HierarchyVictoryPanel = null;
            HierarchyVictoryTitle = null;
            HierarchyVictorySubtitle = null;
            HierarchyRetryButton = null;
            HierarchyNextButton = null;
            HierarchyCaughtPanel = null;
            HierarchyCaughtTitle = null;
            HierarchyCaughtSubtitle = null;
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            CurrentState = state;
        }

        private PlayerMovement CachedPlayer;
        private GUIStyle ReticleStyle;

        private void Start()
        {
            CachedPlayer = FindFirstObjectByType<PlayerMovement>();
        }

        private void EnsureStyles()
        {
            if (CachedPlayer == null)
            {
                CachedPlayer = FindFirstObjectByType<PlayerMovement>();
            }

            if (TitleStyle != null) return;

            TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            TitleStyle.normal.textColor = Color.black;

            SubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            SubtitleStyle.normal.textColor = new Color(0.15f, 0.15f, 0.15f);

            ReticleStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            ReticleStyle.normal.textColor = Color.yellow;

            ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            ButtonStyle.normal.textColor = Color.white;
        }

        private void Update()
        {
            UpdateHierarchyUI();
        }

        private void UpdateHierarchyUI()
        {
            if (HierarchyObjectiveText == null) return;

            bool isLevel2 = GameManager.Instance != null && GameManager.Instance.IsFinalLevel;

            if (CurrentState == GameState.Infiltration)
            {
                int remaining = 0;
                int total = 1;
                if (GameManager.Instance != null)
                {
                    total = GameManager.Instance.TotalObjectivesCount;
                    remaining = total - GameManager.Instance.CollectedObjectivesCount;
                }

                if (!isLevel2)
                {
                    HierarchyObjectiveText.text = "Objective: Steal the Sword. Combat locked until secured.";
                }
                else
                {
                    HierarchyObjectiveText.text = $"Objectives remaining: {remaining}/{total}. Combat locked until all artifacts are secured.";
                }

                if (HierarchySubtitleText != null)
                {
                    HierarchySubtitleText.text = "Maintain stealth and avoid enemy detection.";
                }

                if (HierarchyVictoryPanel != null) HierarchyVictoryPanel.SetActive(false);
                if (HierarchyCaughtPanel != null) HierarchyCaughtPanel.SetActive(false);
            }
            else if (CurrentState == GameState.ObjectiveSecured)
            {
                if (!isLevel2)
                {
                    HierarchyObjectiveText.text = "SWORD READY: Left-click nearby Barbarians to execute a melee takedown.";
                }
                else
                {
                    HierarchyObjectiveText.text = "ARSENAL READY: Left-click Barbarians with Sword | Left-click Flying Mage with Smoke Bomb.";
                }

                if (HierarchySubtitleText != null)
                {
                    HierarchySubtitleText.text = "Extraction Zone is active on the field.";
                }

                if (HierarchyVictoryPanel != null) HierarchyVictoryPanel.SetActive(false);
                if (HierarchyCaughtPanel != null) HierarchyCaughtPanel.SetActive(false);
            }
            else if (CurrentState == GameState.Caught)
            {
                if (HierarchyCaughtPanel != null)
                {
                    HierarchyCaughtPanel.SetActive(true);
                    if (HierarchyCaughtTitle != null) HierarchyCaughtTitle.text = "DETECTED BY ENEMY";
                    if (HierarchyCaughtSubtitle != null) HierarchyCaughtSubtitle.text = "Restarting level...";
                }

                if (HierarchyVictoryPanel != null) HierarchyVictoryPanel.SetActive(false);
            }
            else if (CurrentState == GameState.Escaped)
            {
                if (HierarchyVictoryPanel != null)
                {
                    HierarchyVictoryPanel.SetActive(true);
                    if (HierarchyVictoryTitle != null)
                    {
                        HierarchyVictoryTitle.text = isLevel2 ? "CONGRATULATIONS! HEIST COMPLETE" : "STAGE CLEARED";
                    }
                    if (HierarchyVictorySubtitle != null)
                    {
                        HierarchyVictorySubtitle.text = isLevel2 ? "All contraband secured and sectors cleared." : "Sword retrieved. Proceed to sector 2.";
                    }
                    if (HierarchyNextButton != null)
                    {
                        Text nextBtnText = HierarchyNextButton.GetComponentInChildren<Text>();
                        if (nextBtnText != null)
                        {
                            nextBtnText.text = isLevel2 ? "Play Again" : "Next Contract";
                        }
                    }
                }

                if (HierarchyCaughtPanel != null) HierarchyCaughtPanel.SetActive(false);
            }

            if (HierarchyReticleObject != null)
            {
                if (CurrentState == GameState.ObjectiveSecured && CachedPlayer != null && GameManager.Instance != null && GameManager.Instance.CanEliminateEnemies)
                {
                    string prompt = null;
                    bool inRange = CachedPlayer.IsHoveredEnemyInRange;

                    if (CachedPlayer.HoveredEnemy != null)
                    {
                        prompt = inRange ? "[Left Click] Melee Takedown (Sword)" : "[Sword Takedown] Move Closer";
                    }
                    else if (CachedPlayer.HoveredMage != null)
                    {
                        prompt = inRange ? "[Left Click] Throw Smoke Bomb" : "[Smoke Bomb] Move Closer";
                    }

                    if (!string.IsNullOrEmpty(prompt))
                    {
                        HierarchyReticleObject.SetActive(true);
                        HierarchyReticleObject.transform.position = Input.mousePosition + new Vector3(0f, 40f, 0f);
                        if (HierarchyReticleText != null)
                        {
                            HierarchyReticleText.text = prompt;
                            HierarchyReticleText.color = inRange ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.35f, 0.35f);
                        }
                    }
                    else
                    {
                        HierarchyReticleObject.SetActive(false);
                    }
                }
                else
                {
                    HierarchyReticleObject.SetActive(false);
                }
            }
        }

        private void OnGUI()
        {
            if (HierarchyObjectiveText != null)
            {
                return;
            }

            EnsureStyles();

            bool isLevel2 = GameManager.Instance != null && GameManager.Instance.IsFinalLevel;

            if (CurrentState == GameState.Infiltration)
            {
                int remaining = 0;
                int total = 1;
                if (GameManager.Instance != null)
                {
                    total = GameManager.Instance.TotalObjectivesCount;
                    remaining = total - GameManager.Instance.CollectedObjectivesCount;
                }

                if (!isLevel2)
                {
                    GUI.Label(new Rect(0, 14, Screen.width, 24), "Objective: Steal the Sword. Combat locked until secured.", TitleStyle);
                }
                else
                {
                    GUI.Label(new Rect(0, 14, Screen.width, 24), $"Objectives remaining: {remaining}/{total}. Combat locked until all artifacts are secured.", TitleStyle);
                }
            }
            else if (CurrentState == GameState.ObjectiveSecured)
            {
                TitleStyle.normal.textColor = Color.black;
                if (!isLevel2)
                {
                    GUI.Label(new Rect(0, 14, Screen.width, 24), "SWORD READY: Left-click nearby Barbarians to execute a melee takedown.", TitleStyle);
                }
                else
                {
                    GUI.Label(new Rect(0, 14, Screen.width, 24), "ARSENAL READY: Left-click Barbarians with Sword | Left-click Flying Mage with Smoke Bomb.", TitleStyle);
                }
                TitleStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(0, 38, Screen.width, 20), "Extraction Zone is active on the field.", SubtitleStyle);

                DrawCombatHoverReticle();
            }
            else if (CurrentState == GameState.Caught)
            {
                DrawCaughtScreen();
            }
            else if (CurrentState == GameState.Escaped)
            {
                DrawVictoryScreen();
            }
        }

        private void DrawCombatHoverReticle()
        {
            if (CachedPlayer == null) return;

            string promptText = null;
            bool inRange = CachedPlayer.IsHoveredEnemyInRange;

            if (CachedPlayer.HoveredEnemy != null)
            {
                promptText = inRange ? "[Left Click] Melee Takedown (Sword)" : "[Sword Takedown] Move Closer";
            }
            else if (CachedPlayer.HoveredMage != null)
            {
                promptText = inRange ? "[Left Click] Throw Smoke Bomb" : "[Smoke Bomb] Move Closer";
            }

            if (!string.IsNullOrEmpty(promptText))
            {
                ReticleStyle.normal.textColor = inRange ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);

                Vector3 mousePos = Input.mousePosition;
                float boxWidth = 270;
                float boxHeight = 32;
                float x = Mathf.Clamp(mousePos.x - (boxWidth * 0.5f), 10, Screen.width - boxWidth - 10);
                float y = Mathf.Clamp(Screen.height - mousePos.y - 48, 10, Screen.height - boxHeight - 10);

                GUI.Box(new Rect(x, y, boxWidth, boxHeight), promptText, ReticleStyle);
            }
        }

        private void DrawCaughtScreen()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            TitleStyle.normal.textColor = new Color(1f, 0.25f, 0.25f);
            GUI.Label(new Rect(0, Screen.height * 0.44f, Screen.width, 30), "DETECTED BY ENEMY", TitleStyle);
            TitleStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(0, Screen.height * 0.44f + 32, Screen.width, 20), "Restarting level...", SubtitleStyle);
        }

        private void DrawVictoryScreen()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            bool isFinal = GameManager.Instance != null && GameManager.Instance.IsFinalLevel;

            TitleStyle.normal.textColor = new Color(0.3f, 1f, 0.5f);
            string header = isFinal ? "CONGRATULATIONS! HEIST COMPLETE" : "STAGE CLEARED";
            GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 32), header, TitleStyle);
            TitleStyle.normal.textColor = Color.white;

            string sub = isFinal ? "All contraband secured and sectors cleared." : "Sword retrieved. Proceed to sector 2.";
            GUI.Label(new Rect(0, Screen.height * 0.38f + 36, Screen.width, 22), sub, SubtitleStyle);

            float buttonWidth = 140;
            float buttonHeight = 36;
            float buttonY = Screen.height * 0.38f + 80;

            if (GUI.Button(new Rect((Screen.width * 0.5f) - buttonWidth - 10, buttonY, buttonWidth, buttonHeight), "Restart Level", ButtonStyle))
            {
                GameManager.Instance.RestartCurrentLevel();
            }

            string nextText = isFinal ? "Play Again" : "Next Contract";
            if (GUI.Button(new Rect((Screen.width * 0.5f) + 10, buttonY, buttonWidth, buttonHeight), nextText, ButtonStyle))
            {
                GameManager.Instance.LoadNextLevel();
            }
        }
    }
}
