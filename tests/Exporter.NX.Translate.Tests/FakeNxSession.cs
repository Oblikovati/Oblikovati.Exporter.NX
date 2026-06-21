// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Nx;

namespace Oblikovati.Exporter.NX.Tests
{
    /// <summary>
    /// Named fake for <see cref="INxSession"/> (CLAUDE.md: fakes over inline stubs).
    /// Returns a pre-built NX-neutral document so the translation pipeline can be
    /// exercised with no NX installed.
    /// </summary>
    public sealed class FakeNxSession : INxSession
    {
        private readonly NxDocument _document;

        public FakeNxSession(NxDocument document)
        {
            _document = document;
        }

        public NxDocument ExtractWorkDocument() => _document;
    }
}
