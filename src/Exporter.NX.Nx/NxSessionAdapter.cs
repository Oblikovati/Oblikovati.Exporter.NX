// SPDX-License-Identifier: GPL-2.0-only
using System;
using NXOpen;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Production <see cref="INxSession"/> backed by the live NXOpen session. This is
    /// the single class allowed to read NXOpen types; everything downstream consumes
    /// the NX-neutral <see cref="NxDocument"/>. Extraction grows milestone by milestone
    /// (expressions M2, sketches M3, features M4...). Never exercised in CI — tests use
    /// a fake INxSession instead.
    /// </summary>
    public sealed class NxSessionAdapter : INxSession
    {
        private readonly Session _session;

        public NxSessionAdapter()
            : this(Session.GetSession())
        {
        }

        // Constructor injection keeps the NXOpen session a parameter, per CLAUDE.md.
        public NxSessionAdapter(Session session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public NxDocument ExtractWorkDocument()
        {
            Part? work = _session.Parts.Work;
            if (work == null)
            {
                throw new InvalidOperationException("no work part is open in NX");
            }

            return new NxDocument
            {
                DisplayName = work.Leaf,
                Kind = NxDocumentKind.Part,
                LengthUnit = work.PartUnits == PartUnits.Inches ? "in" : "mm",
                AngleUnit = "deg",
            };
        }
    }
}
