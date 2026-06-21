// SPDX-License-Identifier: GPL-2.0-only
using System;
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class DocumentTranslatorTests
    {
        private static NxDocument MinimalPart()
        {
            return new NxDocument
            {
                DisplayName = "bracket",
                Kind = NxDocumentKind.Part,
                LengthUnit = "mm",
                AngleUnit = "deg",
            };
        }

        [Fact]
        public void TranslatesPartEnvelope()
        {
            OblikovatiDocument doc = new DocumentTranslator().Translate(MinimalPart(), new ExportReport());

            Assert.Equal(2, doc.SchemaVersion);
            Assert.Equal(1, doc.DocumentType);
            Assert.Equal("bracket", doc.DisplayName);
            Assert.IsType<PartRecipe>(doc.Model);
        }

        [Fact]
        public void CarriesUnitsAndParameters()
        {
            NxDocument part = MinimalPart();
            part.LengthUnit = "in";
            part.Expressions.Add(new NxExpression { Name = "width", Formula = "40", Unit = "mm" });

            var recipe = (PartRecipe)new DocumentTranslator().Translate(part, new ExportReport()).Model!;

            Assert.Equal("in", recipe.Units.Length);
            ParameterRecipe width = Assert.Single(recipe.Parameters);
            Assert.Equal("width", width.Name);
            Assert.Equal("user", width.Kind);
            Assert.Equal("40 mm", width.Expression);
        }

        [Fact]
        public void RejectsNonPartDocumentForNow()
        {
            NxDocument asm = MinimalPart();
            asm.Kind = NxDocumentKind.Assembly;

            Assert.Throws<NotSupportedException>(
                () => new DocumentTranslator().Translate(asm, new ExportReport()));
        }
    }
}
