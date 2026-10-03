import hashlib
import json
import shutil
import subprocess
import sys
from pathlib import Path
from run import ROOT, REPO, execute, save

dotnet = REPO / '.dotnet/dotnet.exe'
refs = sorted(Path('C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.1/ref/net10.0').glob('*.dll'))
assert refs
shutil.copytree(ROOT / 'compilers/A', ROOT / 'compilers/B', dirs_exist_ok=True)
shutil.copy2(REPO / 'artifacts/bin/csc/Release/net10.0/Microsoft.CodeAnalysis.CSharp.dll', ROOT / 'compilers/B/Microsoft.CodeAnalysis.CSharp.dll')
manifest = {'baseline': '307e04425fc59844bedc16bed3a8b9ab9ba7b40a', 'candidate': 'baseline + candidate.patch',
            'runtime': '10.0.10', 'referencePack': '10.0.1', 'compilers': {}, 'fixtures': {},
            'references': {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in refs}}
for variant in ['A', 'B']:
    folder = ROOT / 'fixture' / variant
    folder.mkdir(exist_ok=True)
    target = folder / 'Workloads.dll'
    source = ROOT / 'fixture/Workloads.cs'
    rsp = folder / 'Workloads.rsp'
    rsp.write_text('\n'.join(['/nostdlib+', '/target:exe', '/optimize+', '/langversion:preview', '/nullable:enable', '/deterministic+', f'/out:"{target}"'] +
                           [f'/reference:"{p}"' for p in refs] + [f'"{source}"']), encoding='utf-8')
    code = execute(f'compile-{variant}', [str(dotnet), str(ROOT / 'compilers' / variant / 'csc.dll'), '/noconfig', '@' + str(rsp)])
    if code: sys.exit(code)
    save(folder / 'Workloads.runtimeconfig.json', {'runtimeOptions': {'tfm': 'net10.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '10.0.10'}, 'rollForward': 'Disable'}})
    for option in ['--verify', '--list', '--il']:
        result = subprocess.run([str(dotnet), str(target), option], check=True, capture_output=True, text=True)
        save(folder / ('Workloads' + option[1:] + '.json'), json.loads(result.stdout))
        if option == '--verify': print(variant, result.stdout)
    manifest['compilers'][variant] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'compilers'/variant).glob('*.dll')}
    manifest['fixtures'][variant] = {'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'assemblySha256': hashlib.sha256(target.read_bytes()).hexdigest()}
assert [k for k in manifest['compilers']['A'] if manifest['compilers']['A'][k] != manifest['compilers']['B'][k]] == ['Microsoft.CodeAnalysis.CSharp.dll']
save(ROOT / 'manifest.json', manifest)
