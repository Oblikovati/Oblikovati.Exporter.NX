// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Entry;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class ExportRunnerTests
    {
        [Fact]
        public void ProducesOpdFileNameAndYaml()
        {
            var part = new NxDocument { DisplayName = "bracket", Kind = NxDocumentKind.Part };
            var runner = new ExportRunner(
                new FakeNxSession(part),
                new DocumentTranslator(),
                new RecipeYamlWriter());

            ExportResult result = runner.Run();

            Assert.Equal("bracket.opd", result.FileName);
            Assert.Contains("schemaVersion: 2", result.Yaml);
            Assert.Contains("documentType: 1", result.Yaml);
            Assert.Contains("displayName: bracket", result.Yaml);
            Assert.Contains("length: mm", result.Yaml);
            Assert.False(result.Report.HasWarnings);
        }
    }
}
