using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
public class UIManager : MonoBehaviour
{
    private Dictionary<string, GameObject> UIComponents;
    public List<GameObject> componentList;
    public static UIManager Instance { get; private set; }
    
    public GameObject initialPage;

    public GameObject menuCanvas;
    public GameObject gameCanvas;

    private GameObject activeMenu;
    private GameObject activeDialogue;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        UIComponents = new Dictionary<string, GameObject>();
        foreach (GameObject item in componentList)
        {
            UIComponents.Add(item.name, item);
        }
        componentList.Clear();
        initialPage.SetActive(true);
        activeMenu = initialPage;
        activeDialogue = null;
    }

    public void RegisterComponent(string id, GameObject uiElement)
    {
        this.UIComponents.TryAdd(id, uiElement);
    }

    public void UnregisterComponent(string id)
    {
        this.UIComponents.Remove(id);
    }

    public void ShowPanel(string panelId)
    {
        this.UIComponents[panelId].SetActive(true);
    }

    
    public void SwitchWindow(string menuId)
    {
        activeMenu?.SetActive(false);
        activeDialogue?.SetActive(false);
        activeDialogue = null;
        activeMenu = UIComponents[menuId];
        activeMenu.SetActive(true);
    }

    public void SwitchDialogue(string dialogueId)
    {
        activeDialogue?.SetActive(false);
        activeDialogue = UIComponents[dialogueId];
        activeDialogue.SetActive(true);
    }

    public void SwitchCanvas()
    {
        menuCanvas.SetActive(!menuCanvas.activeSelf);
        gameCanvas.SetActive(!gameCanvas.activeSelf);
    }

    public void HideCurrenMenu()
    {
        activeMenu?.SetActive(false);
    }

    public void HideCurrentDialogue()
    {
        activeDialogue?.SetActive(false);
    }

}