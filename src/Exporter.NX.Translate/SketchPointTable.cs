// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    using Slot = System.ValueTuple<long, NxCurvePointRole>;

    /// <summary>
    /// Allocates one distinct recipe point per curve endpoint/center. This mirrors how
    /// the Oblikovati engine itself serializes sketches: each curve keeps its own points
    /// and coincidence is expressed by `coincident` CONSTRAINTS, not by sharing ids
    /// (confirmed by round-tripping an engine-authored rectangle — merging endpoints into
    /// shared ids instead yields zero detected profiles). Coordinates convert from the
    /// IR's millimetres to the recipe's centimetre database unit.
    /// </summary>
    public sealed class SketchPointTable
    {
        private const double MmToCm = 0.1;

        private readonly Dictionary<Slot, int> _slotToPointId = new Dictionary<Slot, int>();
        private readonly List<PointData> _points = new List<PointData>();

        public IReadOnlyList<PointData> Points => _points;

        public int PointId(NxPointRef reference) => _slotToPointId[(reference.CurveId, reference.Role)];

        public void Build(NxSketch sketch, IdAllocator ids)
        {
            foreach (NxCurve curve in sketch.Curves)
            {
                foreach (NxCurvePointRole role in RolesOf(curve.Kind))
                {
                    int id = ids.Next();
                    _slotToPointId[(curve.Id, role)] = id;
                    double[] xy = CoordOf(curve, role);
                    _points.Add(new PointData { Id = id, X = xy[0] * MmToCm, Y = xy[1] * MmToCm });
                }
            }
        }

        private static double[] CoordOf(NxCurve curve, NxCurvePointRole role)
        {
            switch (role)
            {
                case NxCurvePointRole.Start: return curve.Start;
                case NxCurvePointRole.End: return curve.End;
                default: return curve.Center;
            }
        }

        private static IEnumerable<NxCurvePointRole> RolesOf(NxCurveKind kind)
        {
            switch (kind)
            {
                case NxCurveKind.Line:
                    yield return NxCurvePointRole.Start;
                    yield return NxCurvePointRole.End;
                    break;
                case NxCurveKind.Circle:
                    yield return NxCurvePointRole.Center;
                    break;
                default: // Arc
                    yield return NxCurvePointRole.Center;
                    yield return NxCurvePointRole.Start;
                    yield return NxCurvePointRole.End;
                    break;
            }
        }
    }
}
