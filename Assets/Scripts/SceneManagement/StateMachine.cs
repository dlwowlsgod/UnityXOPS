using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEditor;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS의 씬 전환을 담당하는 StateMachine입니다.
    /// </summary>
    /// <remarks>
    /// FSM (유한 상태 기계, Finite State Machine) 모델을 따릅니다.
    /// </remarks>
    public class StateMachine : Singleton<StateMachine>
    {
        public GameState CurrentState { get; private set; } = GameState.None;
        private bool _esc;
        private bool _f12;

        /// <summary>
        /// 다음 씬으로 이동합니다.
        /// </summary>
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
                case GameState.Exit:
#if UNITY_EDITOR
                    EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                    break;
            }
#if UNITY_EDITOR
            Debug.Log($"[StateMachine] State changed: {prevState} -> {CurrentState}");
#endif
        }

        /// <summary>
        /// 다음 State로 이동합니다.
        /// </summary>
        /// <param name="esc">ESC를 누른 상태</param>
        /// <param name="f12">F12를 누른 상태</param>
        public void NextState(bool esc, bool f12)
        {
            _esc = esc;
            _f12 = f12;
            NextState();
        }
        
        /// <summary>
        /// 씬을 비동기 로드합니다.
        /// </summary>
        /// <param name="sceneName">Scene 이름</param>
        /// <returns>코루틴 <see cref="IEnumerator">IEnumerator</see></returns>
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
    /// <see cref="StateMachine">StateMachine</see>의 씬 상태를 나타내는 열거형입니다.
    /// </summary>
    /// <remarks>
    /// 각 값은 게임 생명주기의 특정 단계나 동작에 해당합니다.
    /// Opening, MainMenu, Briefing, Game, Result와 같은 그룹으로 상태가 구성되어 있으며,
    /// 각 그룹은 Start, Update, End 단계로 이루어져 있습니다.
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