using System;
using UnityEngine;

public enum PageType
{
    None = 0,
    FullPage = 1,
    PopUp = 2
}

[Serializable]
public class pageData
{
    public string pageName;
    public Transform parent;
}
