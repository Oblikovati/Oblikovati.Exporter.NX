# SPDX-License-Identifier: GPL-2.0-only
"""Makes the exporter package importable when pytest is run from the repo root."""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
