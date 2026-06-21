// SPDX-License-Identifier: GPL-2.0-only
using Oblikovati.Exporter.NX.Model;
using Oblikovati.Exporter.NX.Recipe;

namespace Oblikovati.Exporter.NX.Translate
{
    /// <summary>
    /// Maps NX expressions to Oblikovati parameter rows. NX keeps the value and unit
    /// apart; Oblikovati carries units inline in the expression string. A numeric
    /// literal formula gets its unit appended ("40" + "mm" -> "40 mm"); a formula that
    /// already references other expressions (e.g. "width * 2") is passed through, since
    /// its units derive from the referenced parameters.
    /// </summary>
    public static class ParameterTranslator
    {
        public static ParameterRecipe Translate(NxExpression expression)
        {
            return new ParameterRecipe
            {
                Name = expression.Name,
                Kind = "user",
                Expression = BuildExpression(expression.Formula, expression.Unit),
            };
        }

        private static string BuildExpression(string formula, string unit)
        {
            string trimmed = formula.Trim();
            if (unit.Length == 0 || !IsNumericLiteral(trimmed))
            {
                return trimmed;
            }

            return trimmed + " " + unit;
        }

        private static bool IsNumericLiteral(string text)
        {
            return double.TryParse(
                text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out _);
        }
    }
}
