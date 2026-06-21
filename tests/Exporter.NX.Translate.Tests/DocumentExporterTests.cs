// SPDX-License-Identifier: GPL-2.0-only
using System.Linq;
using Oblikovati.Exporter.NX.Fixtures;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class DocumentExporterTests
    {
        private static System.Collections.Generic.IReadOnlyList<TranslatedDocument> Export() =>
            new DocumentExporter(new DocumentTranslator()).Export(NxSampleParts.AssemblyDoc(), new ExportReport());

        [Fact]
        public void SharedComponentExportsOnce()
        {
            var files = Export();

            // The .oad plus exactly one component file (the box placed twice).
            Assert.Equal(2, files.Count);
            Assert.Single(files, f => f.FileName == "assembly.oad");
            Assert.Single(files, f => f.FileName == "box-component.opd");
        }

        [Fact]
        public void AssemblyReferencesComponentByNameWithTransforms()
        {
            TranslatedDocument oad = Export().Single(f => f.FileName == "assembly.oad");
            var recipe = Assert.IsType<AssemblyRecipe>(oad.Document.Model);

            Assert.Equal(2, oad.Document.DocumentType); // assembly
            Assert.Equal(2, recipe.Occurrences.Count);
            Assert.All(recipe.Occurrences, o => Assert.Equal("box-component.opd", o.Component));

            // Second occurrence is translated to x = 10 cm (100 mm); cell 3 is the X translation.
            OccurrenceData second = recipe.Occurrences[1];
            Assert.Equal(10.0, second.Transform![3]);
            Assert.Equal(1.0, second.Transform![0]); // identity rotation preserved
        }
    }
}
