using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;
namespace VirulentVentures
{
    public static class CompilerErrorExporter
    {
        // Structure to match LogExporter.cs's LogEntry, with added count
        [System.Serializable]
        private struct LogEntry
        {
            public string timestamp;
            public string type;
            public string message;
            public string stackTrace;
            public int count;
        }
        // Structure to match LogExporter.cs's LogWrapper
        [System.Serializable]
        private struct LogWrapper
        {
            public LogEntry[] logs;
        }
        // Export compiler errors, warnings, and USS validation warnings from Unity's Editor.log to JSON
        [MenuItem("Tools/Export Compiler Errors")]
        private static void ExportCompilerErrors()
        {
            string outputPath = Path.Combine(Application.dataPath, "Logs", "compiler_errors.json");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            string editorLogPath = GetEditorLogPath();
            if (string.IsNullOrEmpty(editorLogPath) || !File.Exists(editorLogPath))
            {
                Debug.LogError($"CompilerErrorExporter: Editor.log not found at {editorLogPath}");
                return;
            }
            try
            {
                // Retry reading file up to 3 times to handle sharing violations
                string[] logLines = null;
                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        using (FileStream fs = new FileStream(editorLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader reader = new StreamReader(fs))
                        {
                            logLines = reader.ReadToEnd().Split('\n').TakeLast(1000).ToArray();
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        retries--;
                        if (retries == 0) throw;
                        System.Threading.Thread.Sleep(100); // Wait before retry
                    }
                }
                // Dictionary to group and count unique messages
                var errorGroups = new Dictionary<string, (string type, int count, string message)>();
                // Scan for C# compiler errors/warnings
                foreach (var line in logLines)
                {
                    if (Regex.IsMatch(line, @"(error|warning) CS\d+:"))
                    {
                        string key = line.Trim(); // Use trimmed line as unique key
                        string type = line.Contains("error CS") ? "Error" : "Warning";
                        if (errorGroups.ContainsKey(key))
                        {
                            var entry = errorGroups[key];
                            entry.count++;
                            errorGroups[key] = entry;
                        }
                        else
                        {
                            errorGroups[key] = (type, 1, line);
                        }
                    }
                }
                // Scan for USS validation warnings
                foreach (var line in logLines)
                {
                    if (Regex.IsMatch(line, @"Assets/UI/.*.uss $line \d+$: warning: Expected"))
                    {
                        string key = line.Trim();
                        string type = "USS Warning";
                        if (errorGroups.ContainsKey(key))
                        {
                            var entry = errorGroups[key];
                            entry.count++;
                            errorGroups[key] = entry;
                        }
                        else
                        {
                            errorGroups[key] = (type, 1, line);
                        }
                    }
                }
                // Convert groups to LogEntries, sorted by first occurrence in log
                var allEntries = errorGroups
                .Select(kv => new { Key = kv.Key, Type = kv.Value.type, Count = kv.Value.count, Message = kv.Value.message })
                .OrderBy(item => Array.IndexOf(logLines, item.Message)) // Preserve approximate log order
                .Select(item => new LogEntry
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    type = item.Type,
                    message = item.Message,
                    stackTrace = "", // Editor.log doesn't provide stack traces
                    count = item.Count
                })
                .ToArray();
                LogWrapper wrapper = new LogWrapper { logs = allEntries };
                if (allEntries.Length == 0)
                {
                    wrapper.logs = new LogEntry[] { new LogEntry
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    type = "Info",
                    message = "No compiler errors, warnings, or USS validation warnings found in recent Editor.log entries.",
                    stackTrace = "",
                    count = 1
                }};
                }
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(outputPath, json);
                Debug.Log($"CompilerErrorExporter: Exported {allEntries.Length} unique entries (with counts) to {outputPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"CompilerErrorExporter: Failed to export to {outputPath}: {e.Message}");
            }
        }
        // Get platform-specific Editor.log path
        private static string GetEditorLogPath()
        {
            string path = "";
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows)
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Logs", "Unity", "Editor.log");
            }
            else if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "unity3d", "Editor.log");
            }
            return path;
        }
    }
}