﻿#if UNITY_EDITOR
namespace Morpeh.Utils.Editor {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;

    /// <summary>
    ///     https://gist.github.com/karljj1/9c6cce803096b5cd4511cf0819ff517b
    /// </summary>
    [InitializeOnLoad]
    public class CompilationTime {
        private const string ASSEMBLY_RELOAD_EVENTS_EDITOR_PREF          = "MORPEH_ASSEMBLY_RELOAD_EVENTS_TIME";
        private const string ASSEMBLY_COMPILATION_EVENTS_EDITOR_PREF     = "MORPEH_ASSEMBLY_COMPILATION_EVENTS";
        private const string ASSEMBLY_TOTAL_COMPILATION_TIME_EDITOR_PREF = "MORPEH_ASSEMBLY_TOTAL_COMPILATION_TIME";

        private static readonly int ScriptAssembliesPathLen = "Library/ScriptAssemblies/".Length;

        private static readonly Dictionary<string, DateTime> StartTimes = new Dictionary<string, DateTime>();

        private static readonly StringBuilder BuildEvents = new StringBuilder();

        private static double compilationTotalTime;

        
        static CompilationTime() {
            CompilationPipeline.assemblyCompilationStarted  += CompilationPipelineOnAssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += CompilationPipelineOnAssemblyCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload       += AssemblyReloadEventsOnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload        += AssemblyReloadEventsOnAfterAssemblyReload;
        }

        private static void CompilationPipelineOnAssemblyCompilationStarted(string assembly) =>
            StartTimes[assembly] = DateTime.UtcNow;

        private static void CompilationPipelineOnAssemblyCompilationFinished(string assembly, CompilerMessage[] arg2) {
            var timeSpan = DateTime.UtcNow - StartTimes[assembly];

            compilationTotalTime += timeSpan.TotalMilliseconds;
            BuildEvents.AppendFormat("  • <i>{0}</i> {1:0.00} seconds\n", assembly.Substring(ScriptAssembliesPathLen, assembly.Length - ScriptAssembliesPathLen),
                timeSpan.TotalMilliseconds / 1000f);
        }


        private static void AssemblyReloadEventsOnBeforeAssemblyReload() {
            var totalCompilationTimeSeconds = compilationTotalTime / 1000f;
            BuildEvents.AppendFormat("────────────────────────────────────\n" +
                                     " <b>Compilation Total:</b> {0:0.00} seconds\n", totalCompilationTimeSeconds);
            EditorPrefs.SetString(ASSEMBLY_RELOAD_EVENTS_EDITOR_PREF, DateTime.UtcNow.ToBinary().ToString());
            EditorPrefs.SetString(ASSEMBLY_COMPILATION_EVENTS_EDITOR_PREF, BuildEvents.ToString());
            EditorPrefs.SetString(ASSEMBLY_TOTAL_COMPILATION_TIME_EDITOR_PREF, totalCompilationTimeSeconds.ToString(CultureInfo.InvariantCulture));
        }

        private static void AssemblyReloadEventsOnAfterAssemblyReload() {
            var binString                   = EditorPrefs.GetString(ASSEMBLY_RELOAD_EVENTS_EDITOR_PREF);
            var totalCompilationTimeString  = EditorPrefs.GetString(ASSEMBLY_TOTAL_COMPILATION_TIME_EDITOR_PREF);
            if(float.TryParse(totalCompilationTimeString, NumberStyles.Any, CultureInfo.InvariantCulture, out var totalCompilationTimeSeconds) == false) {
                return;
            }

            if (long.TryParse(binString, out var bin) == false) {
                return;
            }

            var date             = DateTime.FromBinary(bin);
            var time             = DateTime.UtcNow - date;
            var compilationTimes = EditorPrefs.GetString(ASSEMBLY_COMPILATION_EVENTS_EDITOR_PREF);
            var totalTimeSeconds = totalCompilationTimeSeconds + time.TotalSeconds;
            if (!string.IsNullOrEmpty(compilationTimes)) {
                Debug.Log($"<b>Compilation Report</b>: {totalTimeSeconds:F2} seconds\n" +
                          "────────────────────────────────────\n" +
                          $"{compilationTimes}" +
                          $" <b>Assembly Reload Time:</b> {time.TotalSeconds:F2} seconds\n");
            }
        }
    }
}
#endif