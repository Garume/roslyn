import subprocess
import sys
from run import ROOT, save

# Interleave compilers within each case to reduce the multi-minute gap between pairs.
# Include large/small readonly changes, the initially ambiguous IEnumerable one-item
# result, and both unchanged large/small mutable controls. Retain the initial 25-case run.
cases = ['rolist-where-100000', 'mutable-control-100000', 'rolist-where-1', 'enumerable-where-1', 'mutable-control-1']
save(ROOT / 'confirm-cases.json', cases)
for case in cases:
    filename = f'confirm-case-{case}.json'
    save(ROOT / filename, [case])
    for i, variant in enumerate('ABBA'):
        result = subprocess.run([sys.executable, str(ROOT / 'reproduce/benchmark.py'), f'confirm-{case}-{i}-{variant}', variant, filename, '--long'])
        if result.returncode: sys.exit(result.returncode)
