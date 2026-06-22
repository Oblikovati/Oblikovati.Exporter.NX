// SPDX-License-Identifier: GPL-2.0-only
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NXOpen;
using NXOpen.Assemblies;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Walks an NX component tree into IR occurrences. Each component becomes an
    /// <see cref="NxOccurrence"/> referencing the NX-neutral document of its prototype part
    /// (extracted via the supplied part extractor, deduped by prototype so a part placed N
    /// times exports once). A sub-assembly component recurses; a leaf component extracts its
    /// part. The orientation's columns are the placed X/Y/Z axes (NX convention), so the
    /// row-major 3x3 the IR carries is filled column-wise.
    /// </summary>
    public sealed class ComponentExtractor
    {
        private readonly Func<Part, NxDocument> _extractPart;
        private readonly Dictionary<Part, NxDocument> _docs = new Dictionary<Part, NxDocument>(ReferenceComparer.Instance);

        public ComponentExtractor(Func<Part, NxDocument> extractPart)
        {
            _extractPart = extractPart ?? throw new ArgumentNullException(nameof(extractPart));
        }

        public NxOccurrence Occurrence(Component component)
        {
            component.GetPosition(out Point3d origin, out Matrix3x3 orientation);
            return new NxOccurrence
            {
                Name = component.DisplayName,
                Component = DocumentFor(component.Prototype),
                Position = new[] { origin.X, origin.Y, origin.Z },
                Rotation = RowMajor(orientation),
            };
        }

        private NxDocument DocumentFor(Part prototype)
        {
            if (_docs.TryGetValue(prototype, out NxDocument? existing))
            {
                return existing;
            }

            Component[] children = prototype.ComponentAssembly.RootComponent?.GetChildren() ?? Array.Empty<Component>();
            NxDocument doc = children.Length > 0 ? Subassembly(prototype, children) : _extractPart(prototype);
            _docs[prototype] = doc;
            return doc;
        }

        private NxDocument Subassembly(Part prototype, Component[] children)
        {
            var doc = new NxDocument
            {
                DisplayName = prototype.Leaf,
                Kind = NxDocumentKind.Assembly,
                LengthUnit = prototype.PartUnits == PartUnits.Inches ? "in" : "mm",
            };
            foreach (Component child in children)
            {
                doc.Occurrences.Add(Occurrence(child));
            }

            return doc;
        }

        // Columns of the NX orientation are the placed axes; lay them out row-major.
        private static double[] RowMajor(Matrix3x3 m) => new[]
        {
            m.Xx, m.Yx, m.Zx,
            m.Xy, m.Yy, m.Zy,
            m.Xz, m.Yz, m.Zz,
        };

        private sealed class ReferenceComparer : IEqualityComparer<Part>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(Part? x, Part? y) => ReferenceEquals(x, y);

            public int GetHashCode(Part obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
