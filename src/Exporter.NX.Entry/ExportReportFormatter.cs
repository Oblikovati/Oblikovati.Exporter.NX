// SPDX-License-Identifier: GPL-2.0-only
using System.Text;

namespace Oblikovati.Exporter.NX.Entry
{
    /// <summary>
    /// Renders an <see cref="ExportOutput"/> as the plain-text summary shown to the user
    /// after an export: the files written and any features that could not be translated.
    /// Plain text (a CLI/listing surface), per the logging convention.
    /// </summary>
    public static class ExportReportFormatter
    {
        public static string Summarize(ExportOutput output)
        {
            var text = new StringBuilder();
            text.AppendLine($"Exported {output.Files.Count} document(s) to Oblikovati:");
            foreach (ExportFile file in output.Files)
            {
                text.AppendLine("  " + file.FileName);
            }

            text.AppendLine();
            if (output.Report.HasWarnings)
            {
                text.AppendLine($"{output.Report.Warnings.Count} item(s) need attention:");
                foreach (string warning in output.Report.Warnings)
                {
                    text.AppendLine("  - " + warning);
                }
            }
            else
            {
                text.AppendLine("No warnings — full feature history translated.");
            }

            return text.ToString();
        }
    }
}
