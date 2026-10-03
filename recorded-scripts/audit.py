import hashlib
import json
import subprocess
import sys
from run import ROOT, REPO, execute, save

for variant in ['A', 'B']:
    folder = ROOT / 'fixture' / variant
    rsp = folder / 'SemanticAudit.rsp'
    rsp.write_text((folder / 'Workloads.rsp').read_text().replace('Workloads', 'SemanticAudit'), encoding='utf-8')
    code = execute('audit-compile-' + variant, [str(REPO / '.dotnet/dotnet.exe'), str(ROOT / 'compilers' / variant / 'csc.dll'), '/noconfig', '@' + str(rsp)])
    if code: sys.exit(code)
    (folder / 'SemanticAudit.runtimeconfig.json').write_bytes((folder / 'Workloads.runtimeconfig.json').read_bytes())
    result = subprocess.run([str(REPO / '.dotnet/dotnet.exe'), str(folder / 'SemanticAudit.dll')], check=True, capture_output=True, text=True)
    data = json.loads(result.stdout)
    save(folder / 'SemanticAudit-observations.json', data)
    print(variant, len(data), 'observations')
a, b = [json.loads((ROOT / 'fixture' / variant / 'SemanticAudit-observations.json').read_text()) for variant in ['A','B']]
assert [(x['scenario'], x['target']) for x in a] == [(x['scenario'], x['target']) for x in b]
changes = [{'before': x, 'after': y} for x,y in zip(a,b) if x != y]
for item in b:
    if item['scenario'] == 'null-array': continue
    concrete = next(x for x in b if x['scenario'] == item['scenario'] and x['target'] == 'concrete')
    assert {k:v for k,v in item.items() if k != 'target'} == {k:v for k,v in concrete.items() if k != 'target'}
save(ROOT / 'semantic-comparison.json', {'observationsPerCompiler': len(a), 'candidateMatchesConcreteList': True, 'changes': changes})
print(len(changes), 'changed observations; all candidate enumerable results match concrete List')
