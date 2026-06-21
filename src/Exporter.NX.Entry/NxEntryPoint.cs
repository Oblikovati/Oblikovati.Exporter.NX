// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.IO;
using Oblikovati.Exporter.NX.Nx;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;

namespace Oblikovati.Exporter.NX.Entry
{
    /// <summary>
    /// The method NX invokes from the ribbon button (wired in the .men deploy file).
    /// It is the only place that constructs the real NXOpen-backed adapter and touches
    /// the filesystem; all logic lives in the injectable <see cref="ExportRunner"/>.
    /// The richer ribbon/report UI lands in M7.
    /// </summary>
    public static class NxEntryPoint
    {
        /// <summary>NX managed entry point. Exports the work part next to its source.</summary>
        public static void Main()
        {
            var runner = new ExportRunner(
                new NxSessionAdapter(),
                new DocumentTranslator(),
                new RecipeYamlWriter());

            ExportResult result = runner.Run();
            string outputPath = Path.Combine(
                Path.GetTempPath(), result.FileName);
            File.WriteAllText(outputPath, result.Yaml);
        }

        /// <summary>Required by NX to unload the managed assembly cleanly.</summary>
        public static int GetUnloadOption(string dummy)
        {
            // 1 == Unload immediately after the callback returns.
            return 1;
        }
    }
}
