using UnityEngine;

public class ShowWhenAttribute : PropertyAttribute
{
    public string BoolFieldName { get; }

    public ShowWhenAttribute(string boolFieldName)
    {
        BoolFieldName = boolFieldName;
    }
}
