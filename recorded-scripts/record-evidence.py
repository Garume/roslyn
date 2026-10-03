import hashlib
import json
import shutil
from run import ROOT, REPO, WORKSPACE, save

manifest = json.loads((ROOT / 'manifest.json').read_text())
manifest['candidatePatchSha256'] = hashlib.sha256((ROOT / 'candidate.patch').read_bytes()).hexdigest()
manifest['sourceFiles'] = {name: hashlib.sha256((REPO / name).read_bytes()).hexdigest() for name in [
    'src/Compilers/CSharp/Portable/Lowering/LocalRewriter/LocalRewriter_CollectionExpression.cs',
    'src/Compilers/CSharp/Test/Emit3/Semantics/CollectionExpressionTests.cs']}
driver = WORKSPACE / '.work/roslyn-85777-bdn'
manifest['driver'] = {name: hashlib.sha256((driver / name).read_bytes()).hexdigest() for name in ['Program.cs', 'CpuBench.csproj', 'global.json', 'packages.lock.json', 'bin/Release/net10.0/CpuBench.dll']}
assert (driver / 'Program.cs').read_bytes() == (ROOT / 'reproduce/BdnDriverPrewarm.cs').read_bytes()
for name in ['CpuBench.csproj', 'global.json', 'packages.lock.json']:
    shutil.copy2(driver / name, ROOT / 'reproduce' / name)
manifest['semanticAudit'] = {'sourceSha256': hashlib.sha256((ROOT / 'fixture/SemanticAudit.cs').read_bytes()).hexdigest(),
    'assemblies': {variant: hashlib.sha256((ROOT / 'fixture' / variant / 'SemanticAudit.dll').read_bytes()).hexdigest() for variant in ['A','B']}}
save(ROOT / 'manifest.json', manifest)
a, b = [json.loads((ROOT / 'fixture' / variant / 'Workloads-il.json').read_text()) for variant in ['A', 'B']]
for method in ['mutable', 'list', 'known']:
    assert a[method] == b[method], method
for method in set(b) - {'mutable', 'list', 'known'}:
    assert 'System.Linq.Enumerable::ToList' in b[method]['calls'], method
save(ROOT / 'il-comparison.json', {method: {'before': a[method], 'after': b[method]} for method in a})
print('Compiler, source, driver, semantic hashes and IL control checks recorded.')
