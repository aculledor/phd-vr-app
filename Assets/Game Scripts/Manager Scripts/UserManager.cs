using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserManager : Subject
{
    [System.NonSerialized]
    public List<UserData> users;
    [System.NonSerialized]
    public UserData activeUser;

    private int usedIdentifiers;
    private bool updated;

    protected void Start()
    {

        //Attempt to load users from disk, in case there isn't a user registered,
        //an empty list will be created.
        if (!StorageSystem.LoadUsers(out users))
        {
            users = new List<UserData>();
        }

        //Get info about the number of players already registered in the app-
        usedIdentifiers = PlayerPrefs.HasKey("usedIdentifiers") ? PlayerPrefs.GetInt("usedIdentifiers") : 0;
    }

    public void HandleCallibrationEnd()
    {
        //Save the current user status. In case it doesn't exist, update the registered lists.
        if (SaveActiveUser())
            this.NotifyObservers();
    }

    //Add a new ScoreRecord to the active user.
    public void AddScoreInformation(ScoreRecords scoreRecords)
    {
        this.activeUser.scoreHistory.Add(scoreRecords);

        //If the new value is the highest score so far, update the highestScore
        //field.
        if(scoreRecords.highScore> activeUser.highestScore)
        {
            activeUser.highestScore = scoreRecords.highScore;
            NotifyObservers();
        }

        updated = true;
    }

    internal void ModifyActiveUsername(string name)
    {
        this.activeUser.userName = name;
        this.SaveActiveUser();
        this.NotifyObservers();
    }

    public bool ContainsUserWithName(string text)
    {
        return this.users.FindIndex((item) => item.userName.Equals(text)) > -1;
    }

    public void CreateBasicUserData(string text)
    {
        UserData userData = new UserData(text, $"PATIENT-{usedIdentifiers}");
        activeUser = userData;
    }

    //Adds the active user information to the user list (if it didn't previously exist)
    //or updates an existing user.
    private bool SaveActiveUser()
    {
        bool save = !users.Contains(activeUser);

        if (save)
        {
            users.Add(activeUser);
            usedIdentifiers++;
            PlayerPrefs.SetInt("usedIdentifiers", usedIdentifiers);
            PlayerPrefs.Save();
        }

        updated = true;
        return save;
    }
   
    //Removes data from the selected user
    public void DeleteActiveUser()
    {
        this.users.Remove(this.activeUser);
        this.activeUser = null;
        updated = true;
        this.NotifyObservers();
    }

    //Updates the app active user.
    public void SetActiveUser(int index)
    {
        activeUser = users[index];
    }


    //Assigns all the callibration parameters of the active user.
    public void CallibrateActiveUser(float squattingHeight, float standingHeight, 
        float targetXOffset, float targetYOffset, float punchZOffset)
    {
        UserData userData = this.activeUser;
        userData.squattingHeight = squattingHeight;
        userData.standingHeight = standingHeight;
        userData.targetXOffset = targetXOffset;
        userData.targetYOffset = targetYOffset;
        userData.punchZOffset = punchZOffset;
        updated = true;
    }

    private void OnApplicationPause(bool pause)
    {

        //When the application is paused and the user data has been updated
        //saves it to disk.
        if (pause && updated)
        {
            StorageSystem.SaveUsers(users);
            updated = false;
        }
    }


    //Save the user data in the testing environment
    private void OnApplicationQuit()
    {
        StorageSystem.SaveUsers(users);
    }

    //Adds a custom routine to an active user
    public void AddRoutineToActiveUser(string selectedRoutine)
    {
        activeUser.selectedRoutines.Add(selectedRoutine);
        updated = true;
    }
}
