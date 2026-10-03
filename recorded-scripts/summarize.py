import csv
import json
import math
import re
import statistics as st
import sys
from run import ROOT, save

group = sys.argv[1] if len(sys.argv) > 1 else 'pass'
runs = []
for i, variant in enumerate('ABBA'):
    path = ROOT / 'bdn' / f'{group}-{i}-{variant}' / 'results/CompilerBench-report-full.json'
    paths = [path] if path.exists() else sorted((ROOT / 'bdn').glob(f'{group}-*-{i}-{variant}/results/CompilerBench-report-full.json'))
    assert paths, (group, i, variant)
    entries = [b for p in paths for b in json.loads(p.read_text())['Benchmarks']]
    runs.append({re.search(r'CaseId: "(.+)"', b['FullName']).group(1): b for b in entries})
keys = list(runs[0]); assert all(set(r) == set(keys) for r in runs)
rows = []
for key in keys:
    entries = [r[key] for r in runs]
    means = [b['Statistics']['Mean'] for b in entries]
    ratios = [means[1]/means[0], means[2]/means[3]]
    drifts = [st.median(b['Statistics']['OriginalValues'][:5])/st.median(b['Statistics']['OriginalValues'][-5:]) for b in entries]
    logs = list(map(math.log, ratios))
    center = st.mean(logs); margin = 12.706204736 * st.stdev(logs)/math.sqrt(2)
    rows.append(dict(case=key, baselineNs=st.mean([means[0], means[3]]), candidateNs=st.mean(means[1:3]),
                     ratio=math.exp(center), pairedRatios=ratios, lower95=math.exp(center-margin), upper95=math.exp(center+margin),
                     launchMeansNs=means, firstToLastMedianRatios=drifts,
                     baselineBytes=st.mean([entries[i]['Memory']['BytesAllocatedPerOperation'] for i in [0,3]]),
                     candidateBytes=st.mean([entries[i]['Memory']['BytesAllocatedPerOperation'] for i in [1,2]])))
save(ROOT / f'{group}-comparison.json', {'order': 'ABBA', 'rows': rows,
    'note': 'Exploratory paired log-ratio t intervals with df=1; only two independent pairs. No measurements removed.'})
lines = [f'# {group}: published PR versus centralized revision', '',
         'A = published PR head 307e044; B = local revision. Mean ns/op averages the two launches per compiler. Paired ratio is the geometric mean of B1/A1 and B2/A2, not the quotient of the pooled means. Ratios below 1 favor B. All cases retained.', '',
         '| Case | A ns/op | B ns/op | Paired geometric B/A | Pair ratios | A B/op | B B/op |',
         '| --- | ---: | ---: | ---: | --- | ---: | ---: |']
for r in rows:
    lines.append(f"| {r['case']} | {r['baselineNs']:.2f} | {r['candidateNs']:.2f} | {r['ratio']:.3f} | " +
                 ', '.join(f'{v:.3f}' for v in r['pairedRatios']) + f" | {r['baselineBytes']:.2f} | {r['candidateBytes']:.2f} |")
(ROOT / f'{group}-results.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
with (ROOT / f'{group}-comparison.csv').open('w', newline='', encoding='utf-8') as stream:
    writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
    writer.writeheader(); writer.writerows(rows)
# Flag cases with >10% within-launch drift, a >5% slower geometric ratio,
# or opposing paired direction whose ratios differ by >10%. These are review flags,
# not proof of a regression. Keep the initial run intact if a follow-up is needed.
flagged = [r['case'] for r in rows if r['ratio'] > 1.05 or any(v < 0.9 or v > 1.1 for v in r['firstToLastMedianRatios'])
           or (min(r['pairedRatios']) < 1 < max(r['pairedRatios']) and max(r['pairedRatios']) / min(r['pairedRatios']) > 1.1)]
save(ROOT / f'{group}-flagged.json', flagged)
print('\n'.join(lines)); print('Flagged:', flagged)
