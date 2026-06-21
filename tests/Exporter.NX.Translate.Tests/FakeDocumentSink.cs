// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using Oblikovati.Exporter.NX.Entry;

namespace Oblikovati.Exporter.NX.Tests
{
    /// <summary>
    /// Named fake for <see cref="IDocumentSink"/>: records written files in memory so the
    /// export job can be exercised without touching the filesystem.
    /// </summary>
    public sealed class FakeDocumentSink : IDocumentSink
    {
        public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();

        public void Write(string fileName, string yaml) => Files[fileName] = yaml;
    }
}
