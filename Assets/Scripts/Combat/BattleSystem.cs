using System.Collections.Generic;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Assets.Scripts.Combat
{
    public class BattleSystem : StateMachine
    {
        #region Fields and Properties

        [SerializeField] private BattleUI ui;
        [SerializeField] private Fighter player;
        [SerializeField] private Fighter[] minionTypes;
        [SerializeField] private Fighter bossType;
        [SerializeField] private Button startBattleButton;
        [SerializeField] private VideoController videoController;
        [SerializeField] private GameObject camera;
        [SerializeField] private GameObject FeedbackMessagePrefab;
        [SerializeField] private Transform PlayerHeadTransform;
        [SerializeField] private ExpectedSignsDatabase data;
        [SerializeField] private AchievementManager manager;
        public AchievementManager Manager => manager;
        public ExpectedSignsDatabase Data => data;
        public GameObject feedbackMessagePrefab => FeedbackMessagePrefab;
        public Transform playerHeadTransform => PlayerHeadTransform;

        private Fighter enemy;
        private int currentWave = 0;
        private int currentEnemyIndex = 0;
        private Fighter[] currentWaveEnemies;
        private GamePhase currentPhase = GamePhase.Tutorial;
        private List<string> unusedVocab = new List<string>();

        public Fighter Player => player;
        public Fighter Enemy => enemy;
        public Fighter[] MinionTypes => minionTypes;
        public Fighter BossType => bossType;
        public BattleUI Interface => ui;
        public int CurrentWave => currentWave;
        public State CurrState => State;

        [SerializeField] private SignPromptUI SignUI;
        public SignPromptUI signUI => SignUI;
        [SerializeField] private TextAsset vocabJsonFile;
        private LevelData[] allLevelsData;
        public string[] vocab = {};

        #endregion

        #region Execution

        private void Start()
        {
            player.Reset();
            currentWave = 1;
            SetupWave();

            Interface.InitializePlayer(Player);

            Interface.UpdateWaveText(currentWave);
            SetState(new Begin(this));

            Interface.ShowTutorial();
            LoadVocabData();

            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnStartButton);
            }
            else
            {
                Debug.LogError("Start Battle Button is not assigned in the Inspector!");
            }
        }
        private void LoadVocabData()
        {
            if (vocabJsonFile != null)
            {
                string jsonText = vocabJsonFile.text;
                string wrappedJson = "{\"Levels\":" + jsonText + "}";
                
                VocabDataWrapper wrapper = JsonUtility.FromJson<VocabDataWrapper>(wrappedJson);
                allLevelsData = wrapper.Levels;

                if (allLevelsData != null && allLevelsData.Length > 0)
                {
                    Debug.Log($"Successfully loaded {allLevelsData.Length} vocabulary levels from {vocabJsonFile.name}.");
                }
                else
                {
                    Debug.LogError("Failed to parse vocabulary data or data is empty. Check JSON format.");
                }
            }
            else
            {
                Debug.LogError("Vocab JSON file is not assigned in the Inspector!");
            }
        }

        public string[] GetVocabForCurrentWave()
        {
            if (allLevelsData == null) return new string[0];

            foreach (var level in allLevelsData)
            {
                if (level.level == currentWave)
                {
                    return level.words;
                }
            }
            
            Debug.LogWarning($"No vocab found for wave {currentWave}. Returning empty list.");
            return new string[0];
        }

        private void SetupWave()
        {
            currentEnemyIndex = 0;
            currentWaveEnemies = new Fighter[3];

            for (int i = 0; i < 2; i++)
            {
                int index = Random.Range(0, MinionTypes.Length);
                currentWaveEnemies[i] = MinionTypes[index];
            }

            currentWaveEnemies[2] = BossType;
            unusedVocab.Clear();
        }

        public void InitializeEnemy()
        {
            if (currentEnemyIndex >= 0 && currentEnemyIndex < currentWaveEnemies.Length)
            {
                enemy = currentWaveEnemies[currentEnemyIndex];
                Enemy.Reset();
                vocab = GetVocabForCurrentWave();
            }
        }

        public string GetPseudoRandomWord()
        {
            if (unusedVocab.Count == 0)
            {
                vocab = GetVocabForCurrentWave();
                if (vocab.Length > 0)
                {
                    unusedVocab.AddRange(vocab);
                    Debug.Log("Vocab pool refreshed with all wave words.");
                }
                else
                {
                    Debug.LogError("Vocabulary list is empty for the current wave! Cannot choose word.");
                }
            }

            int randomIndex = Random.Range(0, unusedVocab.Count);
            string wordToSign = unusedVocab[randomIndex];
            
            unusedVocab.RemoveAt(randomIndex);
            
            Debug.Log($"Chosen word for signing: {wordToSign}. Remaining unused: {unusedVocab.Count}");

            return wordToSign;
        }

        public void OnEnemyDefeated()
        {
            currentEnemyIndex++;
            if (currentEnemyIndex >= currentWaveEnemies.Length)
            {
                currentWave++;
                SetupWave();
                Interface.UpdateWaveText(currentWave);
                videoController.UpdateVideoForWave(currentWave);
                currentPhase = GamePhase.Tutorial;
                Interface.ShowTutorial();
            }
        }

        
        public void OnAttackButton()
        {
            StartCoroutine(State.Attack());
        }

        public void OnHealButton()
        {
            StartCoroutine(State.Heal());
        }

        public void OnSpecialButton()
        {
            StartCoroutine(State.Special());
        }

        public void OnDefendButton()
        {
            StartCoroutine(State.Defend());
        }

        public void OnPauseButton()
        {
            Interface.ShowPauseMenu();
        }

        public void OnResumeButton()
        {
            Interface.HidePauseMenu();
        }

        public void OnMainMenuButton()
        {
            SceneManager.LoadScene("Main Menu");
        }

        public void OnRestartButton()
        {
            SceneManager.LoadScene("Main");
        }

        public void OnAchievementButton()
        {
            Interface.ShowAchievementMenu();
        }

        public void OnMenuButton()
        {
            Interface.MainMenu();
        }

        public void OnStartButton()
        {
            if (currentPhase == GamePhase.Tutorial)
            {
                currentPhase = GamePhase.Monster;
                Interface.HideTutorial();
                InitializeEnemy();
            }
            Debug.Log("Battle Started! Transition to the main battle screen.");
        }

        private void TutorialMode()
        {
            Interface.ShowTutorial();
        }

        public void OnBackButton()
        {
            if (Interface.IsAchievementScreenActive())
            {
                Interface.HideAchievementMenu();
            }
            else
            {
                Interface.HidePauseMenu();
            }
        }

        #endregion
    }
}