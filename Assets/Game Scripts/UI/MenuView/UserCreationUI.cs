using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UserCreationUI : MonoBehaviour
{
    public TMP_InputField inputName;
    public Button nextButton;
    public GameObject warningMessage;
    public UserManager userManager;
    // Start is called before the first frame update
    private void OnEnable()
    {
        this.inputName.onValueChanged.AddListener(delegate { CheckElements(); });
        this.inputName.text = "";
    }

    private void OnDisable()
    {
        this.inputName.onValueChanged.RemoveAllListeners();
    }

    void CheckElements()
    {
        bool hasUser = userManager.ContainsUserWithName(inputName.text);


        this.nextButton.interactable = !hasUser && 
            !string.IsNullOrWhiteSpace(inputName.text);

        warningMessage.SetActive(hasUser);
    }

    public void HandleNextElement() {
        userManager.CreateBasicUserData(this.inputName.text);
        UIManager.Instance.SwitchWindow("callibration-dialogue");
    }
}
