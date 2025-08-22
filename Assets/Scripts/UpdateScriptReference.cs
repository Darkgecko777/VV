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
        public string title = "Virulent Ventures Script Reference";
        public string version = "1.7.4";
        public string date;
        public string description = "Auto-generated document tracking all scripts for Virulent Ventures.";
        public List<ScriptEntry> scripts = new List<ScriptEntry>();
        public List<RemovedScriptEntry> removed_scripts = new List<RemovedScriptEntry>();
        public List<string> notes = new List<string>();
    }

    [Serializable]
    public class ScriptEntry
    {
        public string name;
        public string path;
        public string description = "TODO: Add description";
        public List<string> dependencies = new List<string>();
        public string notes = "Auto-generated entry.";
        public string lastModified;
    }

    [Serializable]
    public class RemovedScriptEntry
    {
        public string name;
        public string path;
        public string reason = "Removed during auto-update.";
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

            // Load existing ScriptReference.json if it exists
            ScriptReference scriptReference = new ScriptReference();
            if (File.Exists(outputPath))
            {
                string existingJson = File.ReadAllText(outputPath);
                scriptReference = JsonUtility.FromJson<ScriptReference>(existingJson);
                scriptReference.notes.Add($"Updated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                scriptReference.date = DateTime.Now.ToString("yyyy-MM-dd");
                scriptReference.notes.Add("Initial auto-generated document.");
            }

            // Get all .cs files in Scripts folder
            string[] scriptFiles = Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories);
            List<ScriptEntry> newScripts = new List<ScriptEntry>();
            List<string> currentScriptNames = new List<string>();

            foreach (string filePath in scriptFiles)
            {
                string fileName = Path.GetFileName(filePath);
                currentScriptNames.Add(fileName);

                // Check if script already exists in ScriptReference
                ScriptEntry existingEntry = scriptReference.scripts.Find(s => s.name == fileName);
                if (existingEntry != null)
                {
                    // Update lastModified
                    existingEntry.lastModified = File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd HH:mm:ss");
                    newScripts.Add(existingEntry);
                }
                else
                {
                    // New script entry
                    newScripts.Add(new ScriptEntry
                    {
                        name = fileName,
                        path = filePath,
                        lastModified = File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            // Mark removed scripts
            foreach (var oldScript in scriptReference.scripts)
            {
                if (!currentScriptNames.Contains(oldScript.name))
                {
                    scriptReference.removed_scripts.Add(new RemovedScriptEntry
                    {
                        name = oldScript.name,
                        path = oldScript.path,
                        reason = $"Removed during auto-update on {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    });
                }
            }

            scriptReference.scripts = newScripts;

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
                message = $"UpdateScriptReference: Updated ScriptReference.json with {newScripts.Count} scripts at {outputPath}",
                stackTrace = ""
            });
            File.WriteAllText(debugLogPath, JsonUtility.ToJson(debugLog, true));

            Debug.Log($"UpdateScriptReference: Generated ScriptReference.json with {newScripts.Count} scripts at {outputPath}");
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
    }
}