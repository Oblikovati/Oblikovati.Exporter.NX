// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// The thin seam over a live NX session. The orchestrator depends on this, not on
    /// NXOpen, so it can be driven by a fake in tests. The production implementation is
    /// <see cref="NxSessionAdapter"/>.
    /// </summary>
    public interface INxSession
    {
        /// <summary>
        /// Reads the current work part/assembly into the NX-neutral IR. Throws if no
        /// document is open.
        /// </summary>
        NxDocument ExtractWorkDocument();
    }
}
