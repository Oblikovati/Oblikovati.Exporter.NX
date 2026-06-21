// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Translate;
using Xunit;

namespace Oblikovati.Exporter.NX.Tests
{
    public sealed class ParameterTranslatorTests
    {
        [Fact]
        public void AppendsUnitToNumericLiteral()
        {
            var expr = new NxExpression { Name = "depth", Formula = "12.5", Unit = "mm" };
            Assert.Equal("12.5 mm", ParameterTranslator.Translate(expr).Expression);
        }

        [Fact]
        public void PassesThroughFormulaReferencingOtherParameters()
        {
            var expr = new NxExpression { Name = "twice", Formula = "width * 2", Unit = "mm" };
            // A formula is not a literal, so its units come from referenced params, not appended.
            Assert.Equal("width * 2", ParameterTranslator.Translate(expr).Expression);
        }

        [Fact]
        public void OmitsUnitWhenAbsent()
        {
            var expr = new NxExpression { Name = "count", Formula = "5", Unit = "" };
            Assert.Equal("5", ParameterTranslator.Translate(expr).Expression);
        }
    }
}
