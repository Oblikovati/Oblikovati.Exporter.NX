// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Accumulates what the translator could and could not carry across. Unsupported
    /// NX features are recorded here (never silently dropped, never STEP-substituted)
    /// and surfaced to the user after export.
    /// </summary>
    public sealed class ExportReport
    {
        private readonly List<string> _warnings = new List<string>();

        public IReadOnlyList<string> Warnings => _warnings;

        public bool HasWarnings => _warnings.Count > 0;

        /// <summary>
        /// Records that <paramref name="featureName"/> of NX type
        /// <paramref name="nxType"/> has no translation yet.
        /// </summary>
        public void Unsupported(string nxType, string featureName)
        {
            _warnings.Add($"unsupported NX feature '{featureName}' of type '{nxType}' was skipped");
        }

        public void Warn(string message)
        {
            _warnings.Add(message);
        }
    }
}
