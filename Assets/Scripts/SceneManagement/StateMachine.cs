using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// Represents a state machine responsible for managing transitions between various game states.
    /// </summary>
    /// <remarks>
    /// This class implements a state machine pattern to handle game states and transitions in a structured manner.
    /// It ensures proper entry and exit logic for each state while avoiding redundant transitions to the same state.
    /// </remarks>
    public class StateMachine : Singleton<StateMachine>
    {
        private GameState CurrentState { get; set; } = GameState.None;
#if UNITY_EDITOR
        private float _updateInterval = 10.0f;
        private float _timer;
        private int _iteration;
#endif
        
        private void Update()
        {
            OnStateUpdate(CurrentState);
        }
        
        /// <summary>
        /// Changes the current game state to the specified new state, ensuring proper exit and entry logic is executed.
        /// </summary>
        /// <param name="newState">The new <see cref="GameState"/> to transition to.</param>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            // Exit the current state
            OnStateExit(CurrentState);

            CurrentState = newState;

            // Enter the new state
            OnStateEnter(CurrentState);
        }

        private void OnStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.OpeningStart:
                    StartCoroutine(LoadSceneAsync("Opening"));
                    break;
                case GameState.MainMenuStart:
                    StartCoroutine(LoadSceneAsync("MainMenu"));
                    break;
                case GameState.BriefingStart:
                    StartCoroutine(LoadSceneAsync("Briefing"));
                    break;
                case GameState.GameStart:
                    StartCoroutine(LoadSceneAsync("Game"));
                    break;
                case GameState.ResultStart:
                    StartCoroutine(LoadSceneAsync("Result"));
                    break;
            }
#if UNITY_EDITOR
            _timer = 0.0f;
            _iteration = 0;
            Debug.Log($"[StateMachine] State {state} entered.");
#endif
        }

        private void OnStateUpdate(GameState state)
        { 
            switch (state)
            {
                case GameState.OpeningUpdate:
                    break;
                case GameState.MainMenuUpdate:
                    break;
                case GameState.BriefingUpdate:
                    break;
                case GameState.GameUpdate:
                    break;
                case GameState.ResultUpdate:
                    break;
            }
#if UNITY_EDITOR
            _timer += Time.deltaTime;
            if (_timer >= _updateInterval)
            {
                Debug.Log($"[StateMachine] State {state} updated. Iteration {_iteration++}.");
                _timer = 0.0f;
            }
#endif
        }

        private void OnStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.OpeningEnd:
                    break;
                case GameState.MainMenuEnd:
                    break;
                case GameState.BriefingEnd:
                    break;
                case GameState.GameEnd:
                    break;
                case GameState.ResultEnd:
                    break;
            }
#if UNITY_EDITOR
            Debug.Log($"[StateMachine] State {state} exited.");
#endif
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            var asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            while (asyncLoad is { isDone: false })
            {
                yield return null;
            }

            // After loading the scene, transition to the update state
            switch (sceneName)
            {
                case "Opening":
                    SetState(GameState.OpeningUpdate);
                    break;
                case "MainMenu":
                    SetState(GameState.MainMenuUpdate);
                    break;
                case "Briefing":
                    SetState(GameState.BriefingUpdate);
                    break;
                case "Game":
                    SetState(GameState.GameUpdate);
                    break;
                case "Result":
                    SetState(GameState.ResultUpdate);
                    break;
            }
        }
    }

    /// <summary>
    /// Represents the various states of the game in the state machine.
    /// </summary>
    /// <remarks>
    /// Each value corresponds to a specific phase or behavior of the game lifecycle.
    /// The states are organized into groups such as Opening, MainMenu, Briefing, Game, and Result,
    /// each consisting of Start, Update, and End phases.
    /// </remarks>
    public enum GameState
    {
        None,   
        OpeningStart,
        OpeningUpdate,
        OpeningEnd,
        MainMenuStart,
        MainMenuUpdate,
        MainMenuEnd,
        BriefingStart,
        BriefingUpdate,
        BriefingEnd,
        GameStart,
        GameUpdate,
        GameEnd,
        ResultStart,
        ResultUpdate,
        ResultEnd,
    }
}