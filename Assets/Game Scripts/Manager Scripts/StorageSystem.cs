using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class StorageSystem
{
    public static void SaveUsers(List<UserData> userDataList)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        string path = Application.persistentDataPath + "/user_data.bin";
        FileStream stream = new FileStream(path, FileMode.Create);

        binaryFormatter.Serialize(stream, userDataList);
        stream.Close();
    }

    public static bool LoadUsers(out List<UserData> list)
    {  

        string path = Application.persistentDataPath + "/user_data.bin";
        if (File.Exists(path))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            list = binaryFormatter.Deserialize(stream) as List<UserData>;
            stream.Close();
            return true;
        }

        list = null;
        return false;
    }

    public static bool ExistsLogFile() {
        string path = Application.persistentDataPath + $"/event_log.csv";
        return File.Exists(path);
    }

    public static void SaveResults(string csv_info)
    {
        string outputAppend = "";
        string path = Application.persistentDataPath + $"/event_log.csv";

        if (!File.Exists(path))
        {
            outputAppend += $"Identifier;GameEvent;EventTime;SquatsEnabled;MovementEnabled;" +
                $"TargetsEnabled;CrossPunchEnabled;PatientIdentifier;LeftControllerDistance;" +
                $"RightControllerDistance;SquatHeight;HorizontalMovement{Environment.NewLine}";
        }
        outputAppend += csv_info;

        File.AppendAllText(path, outputAppend);

    }

    public static bool LoadRoutine(string element, out FullRoutine loadedRoutine)
    {

        string path = Application.persistentDataPath + $"/stored_routines/{element}";
        bool existsFile = File.Exists(path);

        if (existsFile)
        {
            loadedRoutine = FullRoutine.CreateFromJSON(File.ReadAllText(path));
        } else
        {
            loadedRoutine = null;
        }

        return existsFile;
    }

    public static bool GetStoredRoutines(out string[] fileNames)
    {
        return ReturnFilesFromSubdirectory("/stored_routines/", "json", out fileNames);
    }

    private static bool ReturnFilesFromSubdirectory(string subdirectory, string fileFormat, out string[] fileNames)
    {
        string path = Application.persistentDataPath + subdirectory;
        bool existsDirectory = Directory.Exists(path);
        List<string> pathNames = new List<string>();

        if (existsDirectory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            FileInfo[] fileInfos = directoryInfo.GetFiles($"*.{fileFormat}");

            foreach (FileInfo info in fileInfos)
            {
                pathNames.Add(info.Name);
            }
        }
        fileNames = pathNames.ToArray();
        return existsDirectory;
    }

    public static byte[] LoadLogFileBinary()
    {
        return File.ReadAllBytes($"{Application.persistentDataPath}/event_log.csv");
    }
}
