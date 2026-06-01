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
        string path = Application.persistentDataPath + "/user_data.bin";

        try
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                binaryFormatter.Serialize(stream, userDataList);
            }

            Debug.Log($"Usuarios guardados correctamente: {userDataList.Count} en {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error guardando usuarios en {path}: {e.Message}");
        }
    }

    public static bool LoadUsers(out List<UserData> list)
    {
        string path = Application.persistentDataPath + "/user_data.bin";

        list = new List<UserData>();

        if (!File.Exists(path))
        {
            Debug.LogWarning($"No existe archivo de usuarios en: {path}");
            return false;
        }

        FileInfo fileInfo = new FileInfo(path);

        if (fileInfo.Length == 0)
        {
            Debug.LogWarning($"El archivo de usuarios existe pero está vacío. Se borra: {path}");
            File.Delete(path);
            return false;
        }

        try
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                object data = binaryFormatter.Deserialize(stream);

                if (data is List<UserData> loadedList)
                {
                    list = loadedList;
                    Debug.Log($"Usuarios cargados correctamente: {list.Count}");
                    return true;
                }

                Debug.LogWarning("El archivo user_data.bin no contiene una List<UserData>. Se ignora.");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"No se pudo cargar user_data.bin. Archivo corrupto o incompatible. Se borra. Error: {e.Message}");

            try
            {
                File.Delete(path);
            }
            catch (Exception deleteException)
            {
                Debug.LogWarning($"No se pudo borrar el archivo corrupto: {deleteException.Message}");
            }

            list = new List<UserData>();
            return false;
        }
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
