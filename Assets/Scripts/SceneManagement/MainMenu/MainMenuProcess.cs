using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// 메인 메뉴의 시간의 흐름에 따른 진행과정을 담당하는 클래스입니다.
    /// </summary>
    public class MainMenuProcess : MonoBehaviour
    {
        [Header("UpDown")]
        [SerializeField]
        private MainMenuButtonFont upButton;
        [SerializeField]
        private MainMenuButtonFont downButton;
        
        [Header("PrevNext")]
        [SerializeField]
        private MainMenuButtonFont prevButton;
        [SerializeField]
        private MainMenuButtonFont nextButton;
        
        [Header("MenuItem")]
        [SerializeField]
        private MainMenuButtonFont[] menuItems;
        
        [Header("Scroll")]
        [SerializeField]
        private ScrollBar scrollBar;
        
        private int _currentMenuPageMax;
        private int _currentMenuPageValue;
        
        private class MissionPage
        {
            public string PageName { get; }
            public IList Missions { get; }
            public int MissionCount => Missions.Count;

            public MissionPage(string pageName, IList missions)
            {
                PageName = pageName;
                Missions = missions;
            }

            public string GetMissionName(int index)
            {
                if (index < 0 || index >= Missions.Count) return "";
                var mission = Missions[index];
                
                if (mission is OfficialMissionParameter officialSo)
                    return officialSo.finalName;
                //구현예정
                if (mission is LegacyAddonMissionParameter legacy)
                    return legacy.finalName;
                
                return "";
            }
        }

        private readonly List<MissionPage> _missionPages = new();
        private int _topVisibleIndex;
        private const int VisibleItemsCount = 8;

        private int _lastSelectedPage;
        private int _lastSelectedMissionIndex;

        private void Start()
        {
            if (StateMachine.Instance.CurrentState == GameState.MainMenuStart)
            {
                var demoCount = ParameterManager.Instance.demoParameters.Count;
                var rand = Random.Range(0, demoCount);
                
                var demoBd1Path = ParameterManager.Instance.demoParameters[rand].bd1Path;
                var demoPd1Path = ParameterManager.Instance.demoParameters[rand].pd1Path;
                var demoSkyIndex = ParameterManager.Instance.demoParameters[rand].skyIndex;
                
                var bd1Path = Path.Combine(Application.streamingAssetsPath, demoBd1Path);
                var pd1Path = Path.Combine(Application.streamingAssetsPath, demoPd1Path);
                
                BD1Reader.Instance.ReadBD1(bd1Path);
                BD1Loader.Instance.LoadBD1(bd1Path);
                
                PD1Reader.Instance.ReadPD1(pd1Path);
                
                SkyManager.Instance.LoadSky(demoSkyIndex);
                
                Clock.Instance.ResetClock();
                
                StateMachine.Instance.NextState(false, false);
            }
            
            InitializeMissionMenu();
            
            if (scrollBar)
            {
                scrollBar.OnScroll += HandleScroll;
            }
        }

        private void OnDestroy()
        {
            if (scrollBar)
            {
                scrollBar.OnScroll -= HandleScroll;
            }
        }

        private void Update()
        {
            HandleInput();

            if (StateMachine.Instance.CurrentState == GameState.MainMenuUpdate)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    StateMachine.Instance.NextState(true, false);
                }
            }

            if (StateMachine.Instance.CurrentState == GameState.MainMenuEnd)
            {
                BD1Loader.Instance.DestroyBD1();
                BD1Reader.Instance.ClearBD1();
                
                PD1Reader.Instance.ClearPD1();
                
                SkyManager.Instance.DestroySky();

                StateMachine.Instance.NextState();
            }

            if (StateMachine.Instance.CurrentState == GameState.Exit)
            {
                StateMachine.Instance.NextState();
            }
        }

        private void InitializeMissionMenu()
        {
            _missionPages.Clear();

            var officialMissions = ParameterManager.Instance.officialMissionParameters;
            if (officialMissions is { Count: > 0 })
            {
                _missionPages.Add(new MissionPage("Official Missions", officialMissions));
            }
            
            var legacyMissions = ParameterManager.Instance.legacyAddonMissionParameters;
            if (legacyMissions is { Count: > 0 })
            {
                _missionPages.Add(new MissionPage("Legacy Addons", legacyMissions));
            }

            if (_missionPages.Count > 0)
            {
                _currentMenuPageMax = _missionPages.Count - 1;
                SwitchToPage(0);
            }
            else
            {
                UpdateMenuView();
            }
        }

        private void SwitchToPage(int pageIndex)
        {
            _currentMenuPageValue = Mathf.Clamp(pageIndex, 0, _currentMenuPageMax);
            _topVisibleIndex = 0;
            UpdateMenuView();
        }

        private void UpdateMenuView()
        {
            if (_missionPages.Count == 0)
            {
                foreach (var item in menuItems) item.UpdateText("");
                UpdateAllButtonsState();
                if(scrollBar.gameObject.activeSelf)
                    scrollBar.gameObject.SetActive(false);
                return;
            }

            MissionPage currentPage = _missionPages[_currentMenuPageValue];
            
            for (int i = 0; i < menuItems.Length; i++)
            {
                int missionIndex = _topVisibleIndex + i;
                if (missionIndex < currentPage.MissionCount)
                {
                    string missionName = currentPage.GetMissionName(missionIndex);
                    menuItems[i].UpdateText(missionName);
                    menuItems[i].IsEnable = true;
                }
                else
                {
                    menuItems[i].UpdateText("");
                    menuItems[i].IsEnable = false;
                }
            }

            scrollBar.UpdateScrollSizeAndConstraint(currentPage.MissionCount);
            if (currentPage.MissionCount > VisibleItemsCount)
            {
                float maxTopIndex = currentPage.MissionCount - VisibleItemsCount;
                float scrollPercentage = maxTopIndex > 0 ? _topVisibleIndex / maxTopIndex : 0;
                scrollBar.SetScrollPosition(scrollPercentage);
            }
            
            UpdateAllButtonsState();
        }
        
        private void UpdateAllButtonsState()
        {
            bool hasMultiplePages = _missionPages.Count > 1;
            prevButton.IsEnable = hasMultiplePages && _currentMenuPageValue > 0;
            nextButton.IsEnable = hasMultiplePages && _currentMenuPageValue < _currentMenuPageMax;

            if (_missionPages.Count > 0)
            {
                MissionPage currentPage = _missionPages[_currentMenuPageValue];
                bool canScroll = currentPage.MissionCount > VisibleItemsCount;
                
                upButton.IsEnable = canScroll && _topVisibleIndex > 0;
                
                int maxTopIndex = currentPage.MissionCount - VisibleItemsCount;
                downButton.IsEnable = canScroll && _topVisibleIndex < maxTopIndex;
            }
            else
            {
                upButton.IsEnable = false;
                downButton.IsEnable = false;
            }
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) OnUp();
            else if (Input.GetKeyDown(KeyCode.DownArrow)) OnDown();
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) OnPrevPage();
            else if (Input.GetKeyDown(KeyCode.RightArrow)) OnNextPage();
        }
        
        private void HandleScroll(float percentage)
        {
            if (_missionPages.Count == 0) return;

            var currentPage = _missionPages[_currentMenuPageValue];
            if (currentPage.MissionCount <= VisibleItemsCount) return;

            var maxTopIndex = currentPage.MissionCount - VisibleItemsCount;
            _topVisibleIndex = Mathf.RoundToInt(maxTopIndex * percentage);
            
            UpdateMenuView();
        }

        
        public void OnPrevPage()
        {
            if (_currentMenuPageValue > 0)
            {
                SwitchToPage(_currentMenuPageValue - 1);
            }
        }

        public void OnNextPage()
        {
            if (_currentMenuPageValue < _currentMenuPageMax)
            {
                SwitchToPage(_currentMenuPageValue + 1);
            }
        }

        public void OnUp()
        {
            if (_missionPages.Count == 0 || _missionPages[_currentMenuPageValue].MissionCount <= VisibleItemsCount) return;

            if (_topVisibleIndex > 0)
            {
                _topVisibleIndex--;
                UpdateMenuView();
            }
        }

        public void OnDown()
        {
            if (_missionPages.Count == 0 || _missionPages[_currentMenuPageValue].MissionCount <= VisibleItemsCount) return;

            MissionPage currentPage = _missionPages[_currentMenuPageValue];
            int maxTopIndex = currentPage.MissionCount - VisibleItemsCount;
            if (_topVisibleIndex < maxTopIndex)
            {
                _topVisibleIndex++;
                UpdateMenuView();
            }
        }
        
        public void OnMissionSelect(int visibleIndex)
        {
            int missionIndex = _topVisibleIndex + visibleIndex;
            if (missionIndex < _missionPages[_currentMenuPageValue].MissionCount)
            {
                _lastSelectedPage = _currentMenuPageValue;
                _lastSelectedMissionIndex = missionIndex;
                
                MissionLoader.Instance.LoadMission(_lastSelectedPage, _lastSelectedMissionIndex);
                
                Clock.Instance.ResetClock();
                
                StateMachine.Instance.NextState(false, false);
            }
        }
    }
}