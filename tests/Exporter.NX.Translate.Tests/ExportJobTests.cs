// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Entry;
using Oblikovati.Exporter.NX.Fixtures;
using Oblikovati.Exporter.NX.Model;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class ExportJobTests
    {
        [Fact]
        public void WritesPartAndReportsNoWarnings()
        {
            var sink = new FakeDocumentSink();
            string summary = ExportJob.Run(new FakeNxSession(NxSampleParts.BoxPart()), sink);

            Assert.True(sink.Files.ContainsKey("box-part.opd"));
            Assert.Contains("Exported 1 document(s)", summary);
            Assert.Contains("No warnings", summary);
        }

        [Fact]
        public void WritesAssemblyAndItsComponents()
        {
            var sink = new FakeDocumentSink();
            ExportJob.Run(new FakeNxSession(NxSampleParts.AssemblyDoc()), sink);

            Assert.True(sink.Files.ContainsKey("assembly.oad"));
            Assert.True(sink.Files.ContainsKey("box-component.opd"));
            Assert.Equal(2, sink.Files.Count);
        }

        [Fact]
        public void SummaryListsUnsupportedFeatureWarnings()
        {
            var part = new NxDocument { DisplayName = "p", Kind = NxDocumentKind.Part };
            part.Features.Add(new UnsupportedKind { Name = "Loft1" });

            string summary = ExportJob.Run(new FakeNxSession(part), new FakeDocumentSink());

            Assert.Contains("need attention", summary);
            Assert.Contains("UnsupportedKind", summary);
        }

        private sealed class UnsupportedKind : NxFeature
        {
        }
    }
}
