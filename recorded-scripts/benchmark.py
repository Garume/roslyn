import json
import os
import sys
from run import ROOT, REPO, WORKSPACE, execute

name, variant = sys.argv[1:3]
cases = json.loads((ROOT / 'cases.json').read_text())
if len(sys.argv) > 3:
    cases = json.loads((ROOT / sys.argv[3]).read_text())
env = os.environ.copy()
env.update(DOTNET_ROOT=str(REPO / '.dotnet'), DOTNET_MULTILEVEL_LOOKUP='0', DOTNET_CLI_TELEMETRY_OPTOUT='1',
           PERFLAB_CPU_ASSEMBLY=str(ROOT / 'fixture' / variant / 'Workloads.dll'), PERFLAB_CPU_CASES=json.dumps(cases),
           PERFLAB_PREWARM_SECONDS='5')
env['PATH'] = str(REPO / '.dotnet') + os.pathsep + env['PATH']
driver = WORKSPACE / '.work/roslyn-85777-bdn'
args = [str(REPO / '.dotnet/dotnet.exe'), str(driver / 'bin/Release/net10.0/CpuBench.dll'), '--filter', '*',
        '--cli', str(REPO / '.dotnet/dotnet.exe'), '--launchCount', '1', '--warmupCount', '15', '--iterationCount', '15',
        '--iterationTime', '500' if '--long' in sys.argv else '200', '--outliers', 'DontRemove', '--join', '--artifacts', str(ROOT / 'bdn' / name)]
sys.exit(execute(name, args, cwd=driver, timeout=3600, env=env))
