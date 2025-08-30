using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VirulentVentures
{
    public static class CompilerErrorExporter
    {
        // Structure to match LogExporter.cs's LogEntry
        [System.Serializable]
        private struct LogEntry
        {
            public string timestamp;
            public string type;
            public string message;
            public string stackTrace;
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

                // Scan for C# compiler errors/warnings
                var csErrorLines = logLines.Select((line, index) => new { Line = line, Index = index })
                    .Where(item => Regex.IsMatch(item.Line, @"(error|warning) CS\d+:"))
                    .Select(item => new LogEntry
                    {
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        type = item.Line.Contains("error CS") ? "Error" : "Warning",
                        message = item.Line,
                        stackTrace = "" // Editor.log doesn't provide stack traces for compiler errors
                    });

                // Scan for USS validation warnings (e.g., "Assets/UI/CombatScene.uss (line \d+): warning: Expected...")
                var ussWarningLines = logLines.Select((line, index) => new { Line = line, Index = index })
                    .Where(item => Regex.IsMatch(item.Line, @"Assets/UI/.*\.uss \(line \d+\): warning: Expected"))
                    .Select(item => new LogEntry
                    {
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        type = "USS Warning",
                        message = item.Line,
                        stackTrace = ""
                    });

                // Combine C# and USS entries, sorted by original log order
                var allEntries = csErrorLines.Concat(ussWarningLines)
                    .OrderBy(entry => Array.IndexOf(logLines, entry.message)) // Preserve log order
                    .ToArray();

                LogWrapper wrapper = new LogWrapper { logs = allEntries };
                if (allEntries.Length == 0)
                {
                    wrapper.logs = new LogEntry[] { new LogEntry
                    {
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        type = "Info",
                        message = "No compiler errors, warnings, or USS validation warnings found in recent Editor.log entries.",
                        stackTrace = ""
                    }};
                }

                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(outputPath, json);
                Debug.Log($"CompilerErrorExporter: Exported {allEntries.Length} entries to {outputPath}");
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