Page Manager Unity Package
===

## Description
This package contains the page manager for full pages and popups: showing and hiding pages, a back stack, and an optional popup animation and sound.

## How to use
First, to fix Git Dependencies, copy the link below and add it to your project via Unity Package Manager: [Installing from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html)
```
https://github.com/sajadimn/noname-oneup-upm-git-dependency-resolver.git
```
Then, copy the link below and add it to your project via Unity Package Manager: [Installing from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html)
```
https://github.com/sajadimn/noname-oneup-page-manager.git
```

## Dependencies
Required: `noname-oneup-event-manager`, `com.unity.ugui`.

Optional, you add them yourself if you want them. The package finds them automatically:

| Package | What it adds | Without it |
|---|---|---|
| `noname-oneup-dotween` | Popup scale and fade animation | The popup appears at once, with no error |
| `noname-oneup-audio` | Popup sound | No sound, with no error |

## Setup
1. Add `PageManager` to a scene object and set its `Canvas`.
2. Give each page a class that inherits from `Page`, with its content as the first child of the page object.
3. Keep popup prefabs in `Resources/PopUps`, and page prefabs in `Resources/Pages` listed in `pageOfResourceByParent`.
4. To play a popup sound, set `popUpSoundName` on `PageManager`. Leave it empty for no sound.

## Examples

✅️ A page
```c#
public class MyPage : Page
{
    protected override void SetPageEvents() { }
    protected override void SetPageProperty() { }
    protected override void ShowPage(object data = null) { }
    protected override void HidePage(object data = null) { }
}
```

✅️ Show a page, and go back
```c#
PageManager.Instance.ShowPageByName("Result", data: isWinner);
PageManager.Instance.ShowPageByName("Settings", saveLast: true);
PageManager.Instance.BackToLastPage();
```

`SetPageEvents` and `SetPageProperty` always run before the first `ShowPage`.

✅️ A page with no background: turn off `isBackground`. The `background` field is hidden in the Inspector and the page does not touch it.

✅️ When back runs with an empty stack, `PageManager.ExitGameStateEvent` is sent through `EventManager`.
