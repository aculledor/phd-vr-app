using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UserEditUI : MonoBehaviour
{
    public TMP_InputField inputName;
    public Button nextButton;
    public GameObject warningMessage;
    public UserManager userManager;
    // Start is called before the first frame update
    private void OnEnable()
    {
        this.inputName.onValueChanged.AddListener(delegate { CheckElements(); });
        this.inputName.text = userManager.activeUser.userName;
    }

    private void OnDisable()
    {
        this.inputName.onValueChanged.RemoveAllListeners();
    }

    void CheckElements()
    {
        bool hasUser = userManager.ContainsUserWithName(inputName.text);
        bool sameName = userManager.activeUser.userName.Equals(inputName.text);

        this.nextButton.interactable = !hasUser &&
            !string.IsNullOrWhiteSpace(inputName.text) && !sameName;

        warningMessage.SetActive(hasUser && !sameName);
    }

    public void HandleNextElement()
    {
        userManager.ModifyActiveUsername(this.inputName.text);
        UIManager.Instance.SwitchWindow("user-management");
    }
}
