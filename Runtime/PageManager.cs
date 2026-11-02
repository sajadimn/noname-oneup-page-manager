using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PageManager : MonoBehaviour
{
    public const string ExitGameStateEvent = "ExitGameStateEvent";
    private const float RetryDelay = 0.01f;

    public static PageManager Instance;

    [HideInInspector] public Page activePage;
    [HideInInspector] public List<Page> pageList;
    [HideInInspector] public Page activePopUp;
    [HideInInspector] public Stack<Page> pageStack = new Stack<Page>();
    private bool adjustingPage;
    public Canvas canvas;
    public List<pageData> pageOfResourceByParent = new List<pageData>();
    [SerializeField] private string popUpSoundName;
    [SerializeField, ReadOnlyField] private string exitGameStateEvent = ExitGameStateEvent;
    public string PopUpSoundName => popUpSoundName;
    private string defaultSortingLayerName;

    private void Awake()
    {
        Instance = this;
        pageList = new List<Page>();
        pageList.Clear();
        defaultSortingLayerName = canvas.sortingLayerName;
    }

    public void AddPage(Page newPage)
    {
        Page foundPage = pageList.Find(page => page.pageName == newPage.pageName && !newPage.isPrefab);
        if (!foundPage)
        {
            RemovePage(newPage);
            pageList.Add(newPage);
        }

        SetPageState(newPage);
    }

    public void RemovePage(Page lastPage)
    {
        Page foundPage = pageList.Find(page => page.pageName == lastPage.pageName);
        if (foundPage)
        {
            if (activePopUp != null && activePopUp.pageName == foundPage.pageName)
                activePopUp = null;
            pageList.Remove(foundPage);
        }
    }

    private void SetPageState(Page newPage)
    {
        if (newPage.isActivated)
        {
            activePage = newPage;
            ChangePageState(newPage.pageName, true);
        }
    }

    public void ShowPageByName(string pageName, bool saveLast = false, object data = null,
        GameObject parent = null,
        bool changeBeforePageState = true,
        bool flushStack = false,
        bool showInCostumeLayer = false, string sortingLayerName = "")
    {
        if (adjustingPage)
        {
            WaitToAdjustingLastPage(pageName, saveLast, data, parent, changeBeforePageState, flushStack,
                showInCostumeLayer, sortingLayerName);
            return;
        }

        if ((activePage != null && activePage.pageName == pageName && activePage.isActivated) ||
            (activePopUp != null && activePopUp.pageName == pageName && activePopUp.isActivated))
            return;

        adjustingPage = true;
        try
        {
            Page currentPage = GetPage(pageName, parent);
            if (currentPage == null)
            {
                Debug.LogError("PageManager - Page not found: " + pageName);
                adjustingPage = false;
            }
            else if (currentPage.pageType == PageType.FullPage)
            {
                HidePage(activePopUp, changeBeforePageState, data);
                HidePage(activePage, changeBeforePageState, data);
                SaveLastPage(saveLast);
                activePopUp = null;
                activePage = currentPage;
                activePage.ChangePageState(true, data);
            }
            else if (currentPage.pageType == PageType.PopUp)
            {
                HidePage(activePopUp, changeBeforePageState, data);
                SaveLastPage(saveLast);
                activePopUp = currentPage;
                activePopUp.ChangePageState(true, data);
            }

            if (currentPage != null)
                AdjustingPageToFree(currentPage.delayToActive);

            if (flushStack && !saveLast)
                pageStack.Clear();

            canvas.sortingLayerName = showInCostumeLayer ? sortingLayerName : defaultSortingLayerName;
        }
        catch (Exception e)
        {
            adjustingPage = false;
            Debug.LogError("PageManager - ShowPageByName Exception: " + e);
        }
    }

    private static void HidePage(Page page, bool changeBeforePageState, object data)
    {
        if (page == null)
            return;

        if (changeBeforePageState)
            page.ChangePageState(false, data);
        else
            page.isActivated = false;
    }

    private void SaveLastPage(bool saveLast)
    {
        if (!saveLast)
            return;

        if (activePopUp != null)
        {
            if (activePage != null && !pageStack.Contains(activePage))
                pageStack.Push(activePage);
            if (pageStack.Count == 0 || pageStack.Peek().pageName != activePopUp.pageName)
                pageStack.Push(activePopUp);
        }
        else if (activePage != null && (pageStack.Count == 0 || pageStack.Peek().pageName != activePage.pageName))
        {
            pageStack.Push(activePage);
        }
    }

    private async void AdjustingPageToFree(float delayTime)
    {
        await Task.Delay(TimeSpan.FromSeconds(delayTime));
        adjustingPage = false;
    }

    private async void WaitToAdjustingLastPage(string pageName, bool saveLast, object data, GameObject parent,
        bool changeBeforePageState, bool flushStack, bool showInCostumeLayer, string sortingLayerName)
    {
        float delay = (activePage != null && activePage.delayToActive > 0) ? activePage.delayToActive : RetryDelay;
        await Task.Delay(TimeSpan.FromSeconds(delay));
        if (this == null)
            return;
        ShowPageByName(pageName, saveLast, data, parent, changeBeforePageState, flushStack, showInCostumeLayer,
            sortingLayerName);
    }

    public void BackToLastPage(object data = null, bool focus = true)
    {
        if (activePopUp != null)
        {
            SetBackToLastFromPopUp(data, focus);
        }
        else if (activePage != null)
        {
            SetBackToLastFromPage(data, focus);
        }
    }

    private void SetBackToLastFromPopUp(object data, bool focus)
    {
        activePopUp.ChangePageState(false, data);
        if (pageStack.Count > 0)
        {
            Page lastPage = pageStack.Peek();
            if (lastPage.pageName == activePopUp.pageName)
            {
                pageStack.Pop();
            }

            lastPage = pageStack.Pop();
            if (lastPage.pageType == PageType.PopUp)
            {
                activePopUp = lastPage;
                activePopUp.ChangePageState(true, data);
            }
            else if (lastPage.pageType == PageType.FullPage)
            {
                if (activePage != null && activePage.pageName != lastPage.pageName)
                {
                    pageStack.Push(lastPage);
                    if (focus)
                        activePage.FocusOnPage(activePopUp);
                    activePopUp = null;
                }
                else if (activePage == null)
                {
                    activePopUp = null;
                    activePage = lastPage;
                    activePage.ChangePageState(true, data);
                }
                else
                {
                    activePage = lastPage;
                    if (focus)
                        activePage.FocusOnPage(activePopUp);
                    activePopUp = null;
                }
            }
        }
        else
        {
            if (activePage != null)
            {
                if (focus)
                    activePage.FocusOnPage(activePopUp);
            }

            activePopUp = null;
        }
    }

    private void SetBackToLastFromPage(object data, bool focus)
    {
        activePopUp = null;
        if (pageStack.Count > 0)
        {
            Page lastPage = pageStack.Pop();
            if (lastPage.pageType == PageType.PopUp)
            {
                Page alternativesPage = null;
                foreach (var page in pageStack)
                {
                    if (page.pageType == PageType.FullPage)
                    {
                        alternativesPage = page;
                        break;
                    }
                }

                if (alternativesPage != null)
                {
                    activePage.ChangePageState(false, data);
                    activePage = alternativesPage;
                    activePage.ChangePageState(true, data);
                }

                activePopUp = lastPage;
                activePopUp.ChangePageState(true, data);
            }
            else if (lastPage.pageType == PageType.FullPage)
            {
                if (activePage != null && activePage.pageName != lastPage.pageName)
                {
                    activePage.ChangePageState(false, data);
                    activePage = lastPage;
                    activePage.ChangePageState(true, data);
                }
                else
                {
                    activePage = lastPage;
                    if (focus)
                        activePage.FocusOnPage();
                }
            }
        }
        else
        {
            EventManager.Instance.SendGlobalEvent(ExitGameStateEvent, null);
        }
    }

    private Page GetPage(string pageName, GameObject parent)
    {
        pageList.RemoveAll(x => x == null);
        Page page = pageList.Find(page => page.pageName == pageName && page != null);
        return page == null ? LoadPage(pageName, parent) : page;
    }

    private Page LoadPage(string pageName, GameObject parent)
    {
        Page page = pageList.Find(page => page.pageName == pageName);

        int resourcePageIndex = pageOfResourceByParent.FindIndex(x => x.pageName == pageName);
        if (page == null && resourcePageIndex != -1)
        {
            string pageAddress = "Pages/" + pageName;
            Page pagePrefab = Resources.Load<Page>(pageAddress);
            if (pagePrefab != null)
            {
                page = Instantiate(pagePrefab, pageOfResourceByParent[resourcePageIndex].parent);
            }
        }

        if (page == null)
        {
            string pageAddress = "PopUps/" + pageName;
            Page pagePrefab = Resources.Load<Page>(pageAddress);
            if (pagePrefab != null)
            {
                page = Instantiate(pagePrefab, parent ? parent.transform : canvas.transform);
                AddPage(page);
            }
        }

        return page;
    }

    public Page GetActivePage()
    {
        Page foundPage = null;
        if (activePopUp != null)
        {
            foundPage = pageList.Find(page => page != null && page.isActivated && page.pageType == PageType.PopUp);
        }

        if (foundPage == null)
        {
            foundPage = pageList.Find(page => page != null && page.isActivated && page.pageType == PageType.FullPage);
        }

        return foundPage;
    }

    public void SetActivePage(Page page, object data = null)
    {
        if ((page == null) ||
            (activePage != null && activePage.pageName == page.pageName && activePage.isActivated) ||
            (activePopUp != null && activePopUp.pageName == page.pageName && activePopUp.isActivated))
            return;

        if (page.pageType == PageType.FullPage)
        {
            if (activePopUp != null)
                activePopUp.ChangePageState(false, data, false);

            if (activePage != null)
                activePage.ChangePageState(false, data, false);

            activePopUp = null;
            activePage = page;
            activePage.ChangePageState(true, data, false);
        }
        else if (page.pageType == PageType.PopUp)
        {
            if (activePopUp != null)
                activePopUp.ChangePageState(false, data, false);

            activePopUp = page;
            activePopUp.ChangePageState(true, data, false);
        }
    }

    public void CloseAllCLosablePopUps()
    {
        if (activePopUp != null)
        {
            if (activePopUp.backState)
            {
                activePopUp.ChangePageState(false);
                activePopUp = null;
            }
        }

        while (true)
        {
            if (pageStack.Count == 0)
                break;
            Page lastPage = pageStack.Peek();
            if (lastPage.pageType == PageType.FullPage)
                break;
            if (lastPage.pageType == PageType.PopUp)
            {
                if (lastPage.backState == false)
                    break;
                if (lastPage.isActivated == false)
                    break;
                pageStack.Pop();
            }
        }
    }

    public void SetActivePage(string pageName, object data = null)
    {
        Page currentPage = pageList.Find(page => page.pageName == pageName);
        SetActivePage(currentPage, data);
    }

    private void ChangePageState(string pageName, bool state)
    {
        Page foundPage = pageList.Find(page => page.pageName == pageName);
        foundPage.ChangePageState(state);
    }

    public Page GetPageByName(string pageName)
    {
        Page foundPage = pageList.Find(page => page.pageName == pageName);
        return foundPage;
    }

    private void OnDestroy()
    {
        Instance = null;
    }
}