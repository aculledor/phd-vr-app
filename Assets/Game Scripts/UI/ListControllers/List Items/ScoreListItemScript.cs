using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreListItemScript : BaseClickableListItem
{

    public TextMeshProUGUI dateMesh;
    public TextMeshProUGUI scoreMesh;

    public void InitializeButton(IClickableListElementCallback listener, string dateString, string scoreString)
    {
        this.button.onClick.AddListener(delegate { listener.ClickCallback(this); });
        dateMesh.text = dateString;
        scoreMesh.text = scoreString;
    }

    protected override void HandleTextSelection()
    {
        dateMesh.color = selectedText;
        scoreMesh.color = selectedText;
    }

    protected override void HandleTextDeselection()
    {
        dateMesh.color = unselectedText;
        scoreMesh.color = unselectedText;
    }
}
