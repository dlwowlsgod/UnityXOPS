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
        public GameState CurrentState { get; private set; } = GameState.None;
        private bool _esc;
        private bool _f12;

        /// <summary>
        /// Advances the state machine to the next state based on the current state and specific conditions.
        /// </summary>
        /// <remarks>
        /// This method transitions the `CurrentState` variable to its later state in the state sequence
        /// or cycles back to the initial state if no valid transition is determined. The state transition logic
        /// is determined based on the enumeration values of `GameState` and certain flags (_f12 and _esc).
        /// Additionally, it performs specific operations when transitioning between certain states, such as
        /// starting coroutines for scene loading.
        /// In Unity's editor mode, this method logs the state transition for debugging purposes.
        /// </remarks>
        public void NextState()
        {
            var prevState = CurrentState;
            switch (CurrentState)
            {
                case GameState.None:
                    StartCoroutine(LoadSceneAsync("Opening"));
                    CurrentState = GameState.OpeningStart;
                    break;
                case GameState.OpeningStart:
                    CurrentState = GameState.OpeningUpdate;
                    break;
                case GameState.OpeningUpdate:
                    CurrentState = GameState.OpeningEnd;
                    break;
                case GameState.OpeningEnd:
                    StartCoroutine(LoadSceneAsync("MainMenu"));
                    CurrentState = GameState.MainMenuStart;
                    break;
                case GameState.MainMenuStart:
                    CurrentState = GameState.MainMenuUpdate;
                    break;
                case GameState.MainMenuUpdate:
                    CurrentState = GameState.MainMenuEnd;
                    break;
                case GameState.MainMenuEnd:
                    if (!_esc)
                    {
                        StartCoroutine(LoadSceneAsync("Briefing"));
                        CurrentState = GameState.BriefingStart;
                    }
                    else
                    {
                        CurrentState = GameState.Exit;
                    }
                    break;
                case GameState.BriefingStart:
                    CurrentState = GameState.BriefingUpdate;
                    break;
                case GameState.BriefingUpdate:
                    CurrentState = GameState.BriefingEnd;
                    break;
                case GameState.BriefingEnd:
                    if (!_esc)
                    {
                        StartCoroutine(LoadSceneAsync("Game"));
                        CurrentState = GameState.GameStart;
                    }
                    else
                    {
                        StartCoroutine(LoadSceneAsync("MainMenu"));
                        CurrentState = GameState.MainMenuStart;
                    }
                    break;
                case GameState.GameStart:
                    CurrentState = GameState.GameUpdate;
                    break;
                case GameState.GameUpdate:
                    CurrentState = GameState.GameEnd;
                    break;
                case GameState.GameEnd:
                    if (_f12)
                    {
                        StartCoroutine(LoadSceneAsync("Game"));
                        CurrentState = GameState.GameStart;
                    }
                    else if (!_esc)
                    {
                        StartCoroutine(LoadSceneAsync("Result"));
                        CurrentState = GameState.ResultStart;
                    }
                    else
                    {
                        StartCoroutine(LoadSceneAsync("MainMenu"));
                        CurrentState = GameState.MainMenuStart;
                    }
                    break;
                case GameState.ResultStart:
                    CurrentState = GameState.ResultUpdate;
                    break;
                case GameState.ResultUpdate:
                    CurrentState = GameState.ResultEnd;
                    break;
                case GameState.ResultEnd:
                    StartCoroutine(LoadSceneAsync("MainMenu"));
                    CurrentState = GameState.MainMenuStart;
                    break;
                default:
                    CurrentState = GameState.None;
                    break;
            }
#if UNITY_EDITOR
            Debug.Log($"[StateMachine] State changed: {prevState} -> {CurrentState}");
#endif
        }

        /// <summary>
        /// Resets specific internal button state flags and triggers a state transition in the state machine.
        /// </summary>
        /// <remarks>
        /// This method sets both `_esc` and `_f12` flags to `false`, effectively clearing any previous button
        /// press states. Following the reset, it invokes the `NextState` method to progress to the next game state
        /// based on the current state and conditions.
        /// </remarks>
        public void AnyKeyFlag()
        {
            _esc = false;
            _f12 = false;
            NextState();
        }

        /// <summary>
        /// Updates the state machine to handle the Escape button input and triggers the corresponding state transition.
        /// </summary>
        /// <remarks>
        /// This method sets the internal escape flag to true and resets the F12 flag to ensure only one action takes precedence.
        /// After updating the flags, it invokes the `NextState` method to perform the state transition logic according to the updated conditions.
        /// This ensures the game reacts appropriately to the Escape button being activated.
        /// </remarks>
        public void EscapeButtonFlag()
        {
            _esc = true;
            _f12 = false;
            NextState();
        }

        /// <summary>
        /// Sets the F12 button activation flag and triggers a transition to the next state.
        /// </summary>
        /// <remarks>
        /// This method updates the internal state by setting the `_f12` flag to true and resetting the `_esc` flag.
        /// It then invokes the `NextState` method to transition the state machine to the later state.
        /// This is typically called when the F12 key is pressed to signal specific game state changes or behaviors.
        /// </remarks>
        public void F12ButtonFlag()
        {
            _esc = false;
            _f12 = true;
            NextState();
        }
        
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            var asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            while (asyncLoad is { isDone: false })
            {
                yield return null;
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
        Exit
    }
}