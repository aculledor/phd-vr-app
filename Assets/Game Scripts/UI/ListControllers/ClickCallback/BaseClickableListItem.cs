using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseClickableListItem : MonoBehaviour
{
    public Button button;

    [Header("Active Button")]
    public Color selectedText;
    public ColorBlock selectedBackground;

    [Header("Inactive Button")]
    public Color unselectedText;
    public ColorBlock unselectedBackground;

    public void SelectButton()
    {
        this.button.colors = selectedBackground;
        HandleTextSelection();
    }

    public void DeselectButton()
    {
        this.button.colors = unselectedBackground;
        HandleTextDeselection();
    }

    private void OnDestroy()
    {
        this.button.onClick.RemoveAllListeners();
    }

    protected abstract void HandleTextSelection();
    protected abstract void HandleTextDeselection();
}
