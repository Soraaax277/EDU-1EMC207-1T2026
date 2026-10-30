using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Midterms
{
    public enum GameState
    {
        Infiltration,
        ObjectiveSecured,
        Caught,
        Escaped
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static event Action<GameState> OnGameStateChanged;

        [SerializeField] private float ReloadDelay = 1.0f;
        [SerializeField] private string NextSceneName = "Level2";

        public GameState CurrentState { get; private set; } = GameState.Infiltration;
        public bool CanEliminateEnemies => CurrentState == GameState.ObjectiveSecured;
        public bool IsFinalLevel => SceneManager.GetActiveScene().name == "Level2";

        public int TotalObjectivesCount { get; private set; }
        public int CollectedObjectivesCount { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            ObjectiveItem[] objectives = FindObjectsByType<ObjectiveItem>(FindObjectsSortMode.None);
            TotalObjectivesCount = objectives.Length;
            CollectedObjectivesCount = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterObjectiveCollected()
        {
            CollectedObjectivesCount++;

            if (CollectedObjectivesCount >= TotalObjectivesCount)
            {
                if (CurrentState == GameState.Infiltration)
                {
                    CurrentState = GameState.ObjectiveSecured;
                    OnGameStateChanged?.Invoke(CurrentState);
                }
            }
        }

        public void OnPlayerCaught()
        {
            if (CurrentState == GameState.Caught || CurrentState == GameState.Escaped) return;

            CurrentState = GameState.Caught;
            OnGameStateChanged?.Invoke(CurrentState);
            StartCoroutine(RestartRoutine());
        }

        public void OnPlayerEscaped()
        {
            if (CurrentState != GameState.ObjectiveSecured) return;

            CurrentState = GameState.Escaped;
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void RestartCurrentLevel()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        public void LoadNextLevel()
        {
            if (IsFinalLevel)
            {
                SceneManager.LoadScene("Level1");
            }
            else
            {
                SceneManager.LoadScene(NextSceneName);
            }
        }

        private IEnumerator RestartRoutine()
        {
            yield return new WaitForSeconds(ReloadDelay);
            RestartCurrentLevel();
        }
    }
}
