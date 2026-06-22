// SPDX-License-Identifier: GPL-2.0-only
using NXOpen;
using Oblikovati.Exporter.NX.Model;

namespace Oblikovati.Exporter.NX.Nx
{
    /// <summary>
    /// Reads a part's user parameters from its NX expressions. Number expressions that no
    /// feature owns are the user parameters (a feature's own values — an extrude distance,
    /// say — are owned by that feature and skipped). The expression's right-hand side is the
    /// formula (it may reference other parameters by name), and its unit becomes the inline
    /// unit on the Oblikovati expression.
    /// </summary>
    public static class ExpressionExtractor
    {
        public static void Extract(Part part, NxDocument document)
        {
            foreach (Expression expression in part.Expressions.ToArray())
            {
                if (expression.Type != "Number" || expression.GetOwningFeature() != null)
                {
                    continue;
                }

                document.Expressions.Add(new NxExpression
                {
                    Name = expression.Name,
                    Formula = expression.RightHandSide,
                    Unit = UnitSymbol(expression),
                });
            }
        }

        private static string UnitSymbol(Expression expression)
        {
            Unit? unit = expression.Units;
            return unit == null ? string.Empty : unit.Symbol;
        }
    }
}
