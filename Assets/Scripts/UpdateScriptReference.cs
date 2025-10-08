using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

namespace VirulentVentures.Editor
{
    [Serializable]
    public class ScriptReference
    {
        public List<string> scripts = new List<string>();
    }

    [Serializable]
    public class DebugLog
    {
        public List<DebugLogEntry> logs;
    }

    [Serializable]
    public class DebugLogEntry
    {
        public string timestamp;
        public string type;
        public string message;
        public string stackTrace;
    }

    public class UpdateScriptReference
    {
        [MenuItem("Tools/Update Script Reference")]
        public static void UpdateReference()
        {
            string scriptsFolder = "Assets/Scripts";
            string outputPath = Path.Combine(Application.dataPath, "Logs", "ScriptReference.json");

            // Ensure Logs directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // Get all .cs files in Scripts folder
            string[] scriptFiles = Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories);
            List<string> scriptNames = new List<string>();

            foreach (string filePath in scriptFiles)
            {
                string fileName = Path.GetFileName(filePath);
                scriptNames.Add(fileName);
            }

            // Create ScriptReference with only script names
            ScriptReference scriptReference = new ScriptReference
            {
                scripts = scriptNames
            };

            // Write to ScriptReference.json
            string json = JsonUtility.ToJson(scriptReference, true);
            File.WriteAllText(outputPath, json);

            // Log to debug_log.json
            string debugLogPath = Path.Combine(Application.dataPath, "Logs", "debug_log.json");
            DebugLog debugLog = new DebugLog { logs = new List<DebugLogEntry>() };
            if (File.Exists(debugLogPath))
            {
                debugLog = JsonUtility.FromJson<DebugLog>(File.ReadAllText(debugLogPath));
            }
            debugLog.logs.Add(new DebugLogEntry
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                type = "Log",
                message = $"UpdateScriptReference: Updated ScriptReference.json with {scriptNames.Count} script names at {outputPath}",
                stackTrace = ""
            });
            File.WriteAllText(debugLogPath, JsonUtility.ToJson(debugLog, true));

            Debug.Log($"UpdateScriptReference: Generated ScriptReference.json with {scriptNames.Count} script names at {outputPath}");
        }
    }
}