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

            ExportOutput result = runner.Run();

            ExportFile file = Assert.Single(result.Files);
            Assert.Equal("bracket.opd", file.FileName);
            Assert.Contains("schemaVersion: 2", file.Yaml);
            Assert.Contains("documentType: 1", file.Yaml);
            Assert.Contains("displayName: bracket", file.Yaml);
            Assert.Contains("length: mm", file.Yaml);
            Assert.False(result.Report.HasWarnings);
        }
    }
}
