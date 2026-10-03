import hashlib
import json
import subprocess
from run import ROOT, REPO, save

for name in ['build-tests-clean', 'build-compiler-final', 'tests-clean', 'format-verify']:
    assert json.loads((ROOT / 'runs' / name / 'execution.json').read_text())['state'] == 'passed', name
receipts = [json.loads(p.read_text()) for p in (ROOT / 'runs').glob('*/execution.json') if p.parent.name.startswith(('pass-', 'confirm-'))]
assert len(receipts) == 24 and all(r['state'] == 'passed' for r in receipts)
manifest = json.loads((ROOT / 'manifest.json').read_text())
for name, digest in manifest['sourceFiles'].items():
    assert hashlib.sha256((REPO / name).read_bytes()).hexdigest() == digest, name
assert hashlib.sha256((ROOT / 'candidate.patch').read_bytes()).hexdigest() == manifest['candidatePatchSha256']
subprocess.run(['git', 'diff', '--check'], cwd=REPO, check=True)
confirm = json.loads((ROOT / 'confirm-comparison.json').read_text())
initial = json.loads((ROOT / 'pass-comparison.json').read_text())
assert len(confirm['rows']) == 5 and len(initial['rows']) == 25
text = ['# Result: review revision, local only', '',
        'Steps 1–5 completed. No push, PR body/title change, or GitHub reply performed.', '',
        '## Code and tests', '',
        '- Shared ToList optimization now covers readonly List-backed wrappers.',
        '- Existing dynamic-Add fast path retained as a fallback outside the general List path; deleting it regressed two existing tests from 7 to 141 bytes of IL.',
        '- Compiler and test builds with analyzers: 0 warnings/errors. Collection-expression suite: 2,058 passed, 7 skipped, 0 failed.',
        '- 25 ordered-result/wrapper checks passed per compiler. 50 semantic observations per compiler; six expected readonly compatibility changes documented in validation.md.', '',
        '## BDN confirmation', '',
        'Windows x64, Ryzen AI 9 HX 370, .NET 10.0.10, BDN 0.15.8. A is published PR head 307e044; B is local candidate.patch. Each row averages two launches per compiler. Confirmations use within-case A/B/B/A and 500 ms requested iterations. No outliers removed.', '',
        '| Case | A ns/op | B ns/op | A B/op | B B/op | Independent pair ratios |',
        '| --- | ---: | ---: | ---: | ---: | --- |']
for r in confirm['rows']:
    text.append(f"| {r['case']} | {r['baselineNs']:.2f} | {r['candidateNs']:.2f} | {r['baselineBytes']:.2f} | {r['candidateBytes']:.2f} | " + ', '.join(f'{v:.3f}' for v in r['pairedRatios']) + ' |')
text += ['', '## Interpretation limits', '',
         'The initial 25-case run had substantial workstation/time drift: unchanged controls also appeared 20–25% faster. Its exact CPU percentages cannot be attributed solely to this change. All initial results remain in pass-results.md and their raw JSON files.', '',
         'The narrower confirmation reduces time separation but does not create an isolated host. Large readonly Where improvements reproduce; small-input CPU results must be read alongside the unchanged control variation. Do not claim universal speedups or absence of CPU regressions on other runtimes/architectures. Allocation reductions in applicable LINQ cases are also recorded independently by MemoryDiagnoser.', '',
         'Full tables: pass-results.md, confirm-results.md. Launch means, paired ratios and exploratory intervals: *-comparison.json. Raw BDN exports: bdn/. Commands, exit status and output: runs/. Exact compiler/source/driver hashes: manifest.json.', '',
         'The original upstream-main comparison is separate in ../roslyn-85777-generalized; this experiment isolates the additional review revision.']
(ROOT / 'outcome.md').write_text('\n'.join(text)+'\n', encoding='utf-8')
save(ROOT / 'completion.json', {'state': 'complete-local', 'steps': [1,2,3,4,5], 'publication': 'deferred-by-user',
    'benchmarkLaunches': 120, 'benchmarkProcessSeconds': sum(r['seconds'] for r in receipts),
    'codeFiles': list(manifest['sourceFiles']), 'benchmarkRuns': [{k:r[k] for k in ['name','state','seconds']} for r in receipts]})
print('\n'.join(text))
