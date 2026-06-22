// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;

namespace Oblikovati.Exporter.NX.Model
{
    /// <summary>
    /// A geometric edge descriptor: the edge's midpoint and direction in model space (mm).
    /// The adapter computes it from an NX edge; Oblikovati binds it to a body edge on
    /// recompute (ADR-0040). Lengths are millimetres (the IR contract).
    /// </summary>
    public sealed class NxEdgeDescriptor
    {
        public double[] Midpoint { get; set; } = { 0, 0, 0 };

        public double[] Direction { get; set; } = { 0, 0, 0 };
    }

    /// <summary>A geometric face descriptor: centroid + outward normal (mm / unit).</summary>
    public sealed class NxFaceDescriptor
    {
        public double[] Centroid { get; set; } = { 0, 0, 0 };

        public double[] Normal { get; set; } = { 0, 0, 1 };
    }

    /// <summary>A fillet rounding the given edges (geometric descriptors) to RadiusMm.</summary>
    public sealed class NxFillet : NxFeature
    {
        public IList<NxEdgeDescriptor> Edges { get; } = new List<NxEdgeDescriptor>();

        public double RadiusMm { get; set; }
    }

    /// <summary>A chamfer bevelling the given edges by DistanceMm (equal distance).</summary>
    public sealed class NxChamfer : NxFeature
    {
        public IList<NxEdgeDescriptor> Edges { get; } = new List<NxEdgeDescriptor>();

        public double DistanceMm { get; set; }
    }

    /// <summary>A shell hollowing the body, removing the given faces, to ThicknessMm.</summary>
    public sealed class NxShell : NxFeature
    {
        public IList<NxFaceDescriptor> RemovedFaces { get; } = new List<NxFaceDescriptor>();

        public double ThicknessMm { get; set; }
    }

    /// <summary>A draft tapering the given faces by AngleDegrees about a pull direction.</summary>
    public sealed class NxDraft : NxFeature
    {
        public IList<NxFaceDescriptor> Faces { get; } = new List<NxFaceDescriptor>();

        public double AngleDegrees { get; set; }

        /// <summary>Pull direction (unit); defaults to +Z.</summary>
        public double[] Pull { get; set; } = { 0, 0, 1 };
    }

    /// <summary>A drilled hole on a placement face (geometric descriptor).</summary>
    public sealed class NxHole : NxFeature
    {
        public NxFaceDescriptor PlacementFace { get; set; } = new NxFaceDescriptor();

        public double DiameterMm { get; set; }

        public double DepthMm { get; set; }

        public bool ThroughAll { get; set; }
    }
}
