using System;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
#if ONEUP_DOTWEEN
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
#endif

public abstract class Page : MonoBehaviour
{
    public string pageName = "";
    public PageType pageType = PageType.FullPage;
    public float delayToActive = (float)0.0;
    public bool isActivated = false;
    public bool isPrefab = false;
    public bool isScale = true;
    public bool backState = true;
    public bool isBackground = true;
    [ShowWhen(nameof(isBackground))] public Image background;
    readonly protected int backgroundFade = 245;
    readonly protected Vector3 transformScale = new Vector3(0.7f, 0.7f, 0.7f);
    private const float SetPageEventsDelay = 0.01f;
    private const float SetPagePropertyDelay = 0.03f;
    private const float ScaleDuration = 0.4f;
    private const float FadeDuration = 0.2f;
    private bool effectState = false;
    private TaskCompletionSource<bool> setupDone;
    private bool showWaitingForSetup;
#if ONEUP_DOTWEEN
    private TweenerCore<Vector3, Vector3, VectorOptions> transformTween;
    private TweenerCore<Color, Color, ColorOptions> backgroundTween;
#endif

    protected virtual void Awake()
    {
        if (PageManager.Instance && !isPrefab)
            PageManager.Instance.pageList.Add(this);
        SetProperty();
    }

    private bool HasBackground => isBackground && background != null;

    private async void SetProperty()
    {
        setupDone = new TaskCompletionSource<bool>();
        try
        {
            if (this == null || this.gameObject == null)
                return;
            ResetAnimation();
            await Task.Delay(TimeSpan.FromSeconds(SetPageEventsDelay));
            if (this == null || !PageManager.Instance)
                return;
            if (!isPrefab)
                PageManager.Instance.AddPage(this);
            SetPageEvents();
            await Task.Delay(TimeSpan.FromSeconds(SetPagePropertyDelay));
            if (this == null)
                return;
            SetPageProperty();
        }
        catch (Exception e)
        {
            Debug.LogError("Page - SetProperty Exception: " + e.Message);
        }
        finally
        {
            setupDone.TrySetResult(true);
        }
    }

    private void SetPageEffect(bool showEffect)
    {
        if (effectState || !isPrefab || this == null || this.transform == null)
            return;
        effectState = true;
        ResetAnimation();
#if ONEUP_DOTWEEN
        if (showEffect)
        {
            if (isScale)
            {
                transformTween?.Kill();
                transformTween = transform.GetChild(0).DOScale(1, ScaleDuration).SetEase(Ease.OutBack);
            }

            backgroundTween?.Kill();
            if (HasBackground)
                backgroundTween = background.DOFade((float)backgroundFade / 255, FadeDuration).SetEase(Ease.Linear)
                    .SetDelay(FadeDuration);
        }
        else
#endif
        {
            if (isScale)
            {
                transform.GetChild(0).localScale = Vector3.one;
            }

            if (HasBackground)
            {
                Color alpha = background.color;
                alpha.a = (float)backgroundFade / 255;
                background.color = alpha;
            }
        }

        if (showEffect)
            PlayPopUpSound();
    }

    private static void PlayPopUpSound()
    {
#if ONEUP_AUDIO
        if (PageManager.Instance && !string.IsNullOrEmpty(PageManager.Instance.PopUpSoundName))
            AudioManager.Instance.Play(AudioCategoryType.SFX, PageManager.Instance.PopUpSoundName);
#endif
    }

    private void ResetAnimation()
    {
        if (isPrefab && pageType == PageType.PopUp)
        {
            if (isScale)
            {
                transform.GetChild(0).transform.localScale = transformScale;
            }

            if (HasBackground)
            {
                Color alpha = background.color;
                alpha.a = 0;
                background.color = alpha;
            }
        }
    }

    private void SetPageUnScale()
    {
        if (!effectState || !isPrefab)
            return;
        effectState = false;
        KillTweens();
        Destroy(transform.gameObject);
    }

    private void KillTweens()
    {
#if ONEUP_DOTWEEN
        backgroundTween?.Kill();
        transformTween?.Kill();
#endif
    }

    private void OnDestroy()
    {
        KillTweens();
        if (PageManager.Instance)
            PageManager.Instance.RemovePage(this);
        if (pageType == PageType.FullPage)
            Destroy();
        if (!effectState || !isPrefab)
            return;
        effectState = false;
        if (PageManager.Instance)
            PageManager.Instance.BackToLastPage();
        if (pageType != PageType.FullPage)
            Destroy();
    }

    public void ChangePageState(bool active, object data = null, bool showEffect = true)
    {
        SetPageActive(active, data, showEffect);
    }

    private async void SetPageActive(bool active, object data = null, bool showEffect = true)
    {
        try
        {
            if (active && showWaitingForSetup)
                return;
            isActivated = active;
            showWaitingForSetup = active && setupDone != null && !setupDone.Task.IsCompleted;
            if (setupDone != null)
                await setupDone.Task;
            showWaitingForSetup = false;
            await Task.Delay(TimeSpan.FromSeconds(delayToActive));
            if (this == null || this.gameObject == null)
                return;
            if (active)
            {
                SetPageEffect(showEffect);
                ShowPage(data);
                Debug.Log("Page - Show Page : " + pageName);
            }
            else
            {
                SetPageUnScale();
                HidePage(data);
                Debug.Log("Page - Hide Page : " + pageName);
            }

            if (this == null || this.gameObject == null)
                return;
            transform.GetChild(0).gameObject.SetActive(active);
        }
        catch (Exception e)
        {
            Debug.LogError("Page - SetPageActive Exception: " + e.Message);
        }
    }

    protected abstract void SetPageEvents();
    protected abstract void SetPageProperty();
    protected abstract void ShowPage(object data = null);
    protected abstract void HidePage(object data = null);

    public virtual void Back()
    {
        PageManager.Instance.BackToLastPage();
    }

    public virtual void FocusOnPage(object data = null)
    {
    }

    protected virtual void Destroy()
    {
    }
}