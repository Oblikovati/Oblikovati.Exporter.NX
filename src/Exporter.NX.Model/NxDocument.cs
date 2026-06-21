// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.NX.Model
{
    /// <summary>
    /// Kind of an extracted NX document. Mirrors Oblikovati's document types so the
    /// translator can pick the right recipe envelope (.opd vs .oad).
    /// </summary>
    public enum NxDocumentKind
    {
        Part = 1,
        Assembly = 2,
    }

    /// <summary>
    /// The NX-neutral root of one extracted document. The NXOpen adapter populates
    /// this from a live session; the translator consumes only this (never NXOpen).
    ///
    /// Example:
    /// <code>
    /// var doc = new NxDocument { DisplayName = "bracket", Kind = NxDocumentKind.Part };
    /// </code>
    /// </summary>
    public sealed class NxDocument
    {
        public string DisplayName { get; set; } = string.Empty;

        public NxDocumentKind Kind { get; set; } = NxDocumentKind.Part;

        /// <summary>Length unit abbreviation as reported by NX (e.g. "mm", "in").</summary>
        public string LengthUnit { get; set; } = "mm";

        /// <summary>Angle unit abbreviation as reported by NX (e.g. "deg", "rad").</summary>
        public string AngleUnit { get; set; } = "deg";

        /// <summary>User/model expressions extracted from the part, in NX order.</summary>
        public IList<NxExpression> Expressions { get; } = new List<NxExpression>();

        /// <summary>2D sketches extracted from the part, in creation order.</summary>
        public IList<NxSketch> Sketches { get; } = new List<NxSketch>();
    }

    /// <summary>
    /// One NX expression (the NX equivalent of an Oblikovati parameter). The
    /// <see cref="Formula"/> is the raw NX right-hand side and may reference other
    /// expressions by name (e.g. "width * 2").
    /// </summary>
    public sealed class NxExpression
    {
        public string Name { get; set; } = string.Empty;

        public string Formula { get; set; } = string.Empty;

        /// <summary>Unit abbreviation NX associates with the expression value.</summary>
        public string Unit { get; set; } = string.Empty;
    }
}
