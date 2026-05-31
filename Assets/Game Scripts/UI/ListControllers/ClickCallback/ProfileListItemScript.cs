using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ProfileListItemScript : BaseClickableListItem
{
    public TextMeshProUGUI profileMesh;

    public void InitializeButton(IClickableListElementCallback listener, RoutinePresets preset)
    {
        this.button.onClick.AddListener(delegate { listener.ClickCallback(this); });
        profileMesh.text = preset.identifier;
    }

    protected override void HandleTextSelection()
    {
        profileMesh.color = selectedText;
    }

    protected override void HandleTextDeselection()
    {
        profileMesh.color = unselectedText;
    }
}
