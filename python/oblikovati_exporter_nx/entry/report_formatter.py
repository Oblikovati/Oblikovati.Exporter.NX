# SPDX-License-Identifier: GPL-2.0-only
"""Renders an ExportOutput as the plain-text summary shown to the user after an export.

Plain text (a CLI/listing surface), per the logging convention.
"""
from __future__ import annotations

from .export_runner import ExportOutput


def summarize(output: ExportOutput) -> str:
    lines = [f"Exported {len(output.files)} document(s) to Oblikovati:"]
    for file in output.files:
        lines.append("  " + file.file_name)
    lines.append("")
    if output.report.has_warnings:
        warnings = output.report.warnings
        lines.append(f"{len(warnings)} item(s) need attention:")
        for warning in warnings:
            lines.append("  - " + warning)
    else:
        lines.append("No warnings — full feature history translated.")
    return "\n".join(lines) + "\n"
