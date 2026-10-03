"""Run a recorded, bounded local Roslyn validation phase."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parent.parent
WORKSPACE = ROOT.parents[3]
REPO = WORKSPACE / '.work/roslyn-85777'

def save(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')

def execute(name, args, cwd=REPO, timeout=3600, env=None):
    args = list(args)
    if (cwd / args[0]).is_file():
        args[0] = str(cwd / args[0])
    folder = ROOT / 'runs' / name
    folder.mkdir(parents=True, exist_ok=False)
    receipt = {'name': name, 'command': [str(a) for a in args], 'cwd': str(cwd),
               'startedAt': datetime.datetime.now(datetime.timezone.utc).isoformat(),
               'timeoutSeconds': timeout, 'state': 'running'}
    save(folder / 'execution.json', receipt)
    started = time.monotonic()
    with (folder / 'output.log').open('wb') as log:
        try:
            process = subprocess.Popen(receipt['command'], cwd=cwd, stdout=log, stderr=subprocess.STDOUT, env=env)
        except OSError as error:
            receipt.update(state='launch_failed', reason=str(error))
            save(folder / 'execution.json', receipt)
            raise
        receipt['pid'] = process.pid
        save(folder / 'execution.json', receipt)
        try:
            code = process.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'], capture_output=True)
            receipt['state'] = 'timed_out'
            code = -1
    receipt.update(exitCode=code, seconds=time.monotonic()-started,
                   finishedAt=datetime.datetime.now(datetime.timezone.utc).isoformat())
    if receipt['state'] == 'running':
        receipt['state'] = 'passed' if code == 0 else 'failed'
    save(folder / 'execution.json', receipt)
    print(json.dumps(receipt))
    print((folder / 'output.log').read_text(encoding='utf-8', errors='replace')[-5000:])
    return code

if __name__ == '__main__':
    sys.exit(execute(sys.argv[1], sys.argv[2:]))
