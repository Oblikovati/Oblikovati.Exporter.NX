# SPDX-License-Identifier: GPL-2.0-only
"""Reads a part's user parameters from its NX expressions.

Number expressions that no feature owns are the user parameters (a feature's own
values — an extrude distance, say — are owned by that feature and skipped). The
expression's right-hand side is the formula (it may reference other parameters by name),
and its unit becomes the inline unit on the Oblikovati expression.

UNVERIFIED — needs a real NX session; NXOpen member shapes follow the documented API.
"""
from __future__ import annotations

from ..model.document import NxDocument, NxExpression


def extract(part, document: NxDocument) -> None:
    for expression in part.Expressions:
        if expression.Type != "Number" or expression.GetOwningFeature() is not None:
            continue
        document.expressions.append(
            NxExpression(
                name=expression.Name,
                formula=expression.RightHandSide,
                unit=_unit_symbol(expression),
            )
        )


def _unit_symbol(expression) -> str:
    unit = expression.Units
    return "" if unit is None else unit.Symbol
