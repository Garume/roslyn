import subprocess
import sys
from pathlib import Path

folder = Path(__file__).resolve().parent
for script in ['compile-fixtures.py', 'audit.py']:
    result = subprocess.run([sys.executable, str(folder / script)])
    if result.returncode: sys.exit(result.returncode)
for i, variant in enumerate('ABBA'):
    result = subprocess.run([sys.executable, str(folder / 'benchmark.py'), f'pass-{i}-{variant}', variant])
    if result.returncode: sys.exit(result.returncode)
