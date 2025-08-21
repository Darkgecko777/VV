using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class LogExporter : MonoBehaviour
    {
        private string logFilePath;
        private List<LogEntry> logEntries = new List<LogEntry>();

        [System.Serializable]
        private struct LogEntry
        {
            public string timestamp;
            public string type;
            public string message;
            public string stackTrace;
        }

        [System.Serializable]
        private struct LogWrapper
        {
            public List<LogEntry> logs;
        }

        void OnEnable()
        {
            logFilePath = Path.Combine(Application.dataPath, "Logs", "debug_log.json");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
            try
            {
                File.WriteAllText(logFilePath, "{\"logs\":[]}");
            }
            catch (Exception)
            {
                // Silently handle clear failure
            }
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            SaveLogsToFile();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                SaveLogsToFile();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                // Could add a minimal log here if count check is critical, but keeping it clean per request
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            logEntries.Add(new LogEntry
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                type = type.ToString(),
                message = logString,
                stackTrace = stackTrace
            });
        }

        private void SaveLogsToFile()
        {
            try
            {
                LogWrapper wrapper = new LogWrapper { logs = logEntries };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(logFilePath, json);
            }
            catch (Exception)
            {
                // Fallback to persistentDataPath
                string fallbackPath = Path.Combine(Application.persistentDataPath, "Logs", "debug_log.json");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath));
                    File.WriteAllText(fallbackPath, JsonUtility.ToJson(new LogWrapper { logs = logEntries }, true));
                }
                catch
                {
                    // Silently handle fallback failure
                }
            }
        }
    }
}