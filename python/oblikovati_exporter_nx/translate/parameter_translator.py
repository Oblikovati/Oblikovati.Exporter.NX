# SPDX-License-Identifier: GPL-2.0-only
"""Maps NX expressions to Oblikovati parameter rows.

NX keeps the value and unit apart; Oblikovati carries units inline in the expression
string. A numeric literal formula gets its unit appended ("40" + "mm" -> "40 mm"); a
formula that already references other expressions (e.g. "width * 2") is passed through,
since its units derive from the referenced parameters.
"""
from __future__ import annotations

from ..model.document import NxExpression
from ..recipe.parameter import ParameterRecipe


def translate(expression: NxExpression) -> ParameterRecipe:
    return ParameterRecipe(
        name=expression.name,
        kind="user",
        expression=_build_expression(expression.formula, expression.unit),
    )


def _build_expression(formula: str, unit: str) -> str:
    trimmed = formula.strip()
    if not unit or not _is_numeric_literal(trimmed):
        return trimmed
    return trimmed + " " + unit


def _is_numeric_literal(text: str) -> bool:
    try:
        float(text)
        return True
    except ValueError:
        return False
