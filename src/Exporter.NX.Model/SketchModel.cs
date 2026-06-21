// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.NX.Model
{
    /// <summary>
    /// An NX sketch in NX-neutral terms: a plane plus curves, geometric constraints and
    /// dimensions that reference those curves by their NX id. The translator turns this
    /// into Oblikovati's shared-point sketch model. All lengths are in MILLIMETRES (the
    /// adapter normalises NX's part units); the translator converts to the recipe's
    /// centimetre database unit.
    /// </summary>
    public sealed class NxSketch
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Plane origin in model space (mm).</summary>
        public double[] Origin { get; set; } = { 0, 0, 0 };

        /// <summary>In-plane X axis (unit vector, model space).</summary>
        public double[] XAxis { get; set; } = { 1, 0, 0 };

        /// <summary>In-plane Y axis (unit vector, model space).</summary>
        public double[] YAxis { get; set; } = { 0, 1, 0 };

        public IList<NxCurve> Curves { get; } = new List<NxCurve>();

        public IList<NxSketchConstraint> Constraints { get; } = new List<NxSketchConstraint>();

        public IList<NxSketchDimension> Dimensions { get; } = new List<NxSketchDimension>();
    }

    public enum NxCurveKind
    {
        Line,
        Circle,
        Arc,
    }

    /// <summary>Which defining point of a curve a constraint/dimension refers to.</summary>
    public enum NxCurvePointRole
    {
        Start,
        End,
        Center,
    }

    /// <summary>
    /// One sketch curve. Coordinates are 2D in sketch space (mm). A line uses
    /// Start/End; a circle uses Center/Radius; an arc uses Center/Start/End plus Ccw.
    /// </summary>
    public sealed class NxCurve
    {
        public long Id { get; set; }

        public NxCurveKind Kind { get; set; }

        public double[] Start { get; set; } = { 0, 0 };

        public double[] End { get; set; } = { 0, 0 };

        public double[] Center { get; set; } = { 0, 0 };

        public double Radius { get; set; }

        public bool Ccw { get; set; }

        public bool Construction { get; set; }
    }

    /// <summary>A reference to one defining point of a curve (e.g. a line's end point).</summary>
    public readonly struct NxPointRef
    {
        public NxPointRef(long curveId, NxCurvePointRole role)
        {
            CurveId = curveId;
            Role = role;
        }

        public long CurveId { get; }

        public NxCurvePointRole Role { get; }
    }

    public enum NxConstraintKind
    {
        Coincident,
        Horizontal,
        Vertical,
        Parallel,
        Perpendicular,
        Collinear,
        EqualLength,
        Concentric,
        EqualRadius,
        Tangent,
        PointOnLine,
        Midpoint,
        Fix,
    }

    /// <summary>
    /// One geometric constraint. <see cref="Points"/> carries point-ref operands;
    /// <see cref="Curves"/> carries curve-id operands. The translator knows which to use
    /// per kind (e.g. parallel uses two curves; coincident uses two points).
    /// </summary>
    public sealed class NxSketchConstraint
    {
        public NxConstraintKind Kind { get; set; }

        public IList<NxPointRef> Points { get; } = new List<NxPointRef>();

        public IList<long> Curves { get; } = new List<long>();
    }

    public enum NxDimensionKind
    {
        Distance,
        Radius,
        Diameter,
        Angle,
    }

    /// <summary>
    /// One dimensional constraint. <see cref="Expression"/> is the parameter expression
    /// driving it (e.g. "width" or "40 mm"); a driven (reference) dimension measures
    /// rather than drives.
    /// </summary>
    public sealed class NxSketchDimension
    {
        public NxDimensionKind Kind { get; set; }

        public IList<NxPointRef> Points { get; } = new List<NxPointRef>();

        public IList<long> Curves { get; } = new List<long>();

        public string Expression { get; set; } = string.Empty;

        public bool Driven { get; set; }
    }
}
