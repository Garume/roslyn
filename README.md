# Roslyn #85777: generalized single-spread lowering

Benchmark and behavioral evidence for [PR #85777](https://github.com/dotnet/roslyn/pull/85777).
This branch only holds evidence; its files are not part of the product PR.

Baseline: `8ddc2f18d7a23028823d0870abf60e4b61cda24e`.
Published candidate: `307e04425fc59844bedc16bed3a8b9ab9ba7b40a`.
The published and locally validated source trees are identical:
`f5969b51aa882ec99bd41db7fa3aff816d77130d`.
Compiler and fixture SHA-256 hashes are in [manifest.json](manifest.json).
Only Microsoft.CodeAnalysis.CSharp.dll differs between the saved compiler pairs;
csc.dll and Microsoft.CodeAnalysis.dll match byte for byte.

## Results

Windows 11 x64, AMD Ryzen AI 9 HX 370, .NET 10.0.10, workstation GC,
BenchmarkDotNet 0.15.8 with MemoryDiagnoser, affinity to logical CPU 0.
Driver SDK: 11.0.100-rc.1.26425.128. Both fixture compilations used the same
net10.0 reference pack 10.0.1 and the same source. The BDN driver loads the
separately compiled fixture assemblies. Setup and reflection are outside timing;
the same delegate wrapper remains in both measured variants.

The table below uses the 13-case follow-up only. Values are arithmetic means
of two independent launch means per compiler, in A/B/B/A order. Each launch
executes the operation for five seconds in GlobalSetup, then uses 15 warmup and
15 measured iterations, with a requested iteration time of 200 ms. Outliers
are retained. Where predicates in these selected rows accept every element;
collection-expression targets are IList<int>.

| Case | Before | After | Before B/op | After B/op |
| --- | ---: | ---: | ---: | ---: |
| Where, 100,000 items | 300.44 us | 97.51 us | 1,049,206 | 400,187 |
| Where.Select, 100,000 items | 313.99 us | 105.02 us | 1,049,282 | 400,225 |
| Where, 8 items | 27.73 ns | 14.95 ns | 176 | 136 |
| Where, 1 item | 14.46 ns | 14.70 ns | 120 | 112 |
| AddRange control, 100,000 items | 304.81 us | 303.45 us | 1,049,214 | 1,049,192 |

The one-item CPU comparison is inconclusive: the two paired after/before ratios
were 1.167 and 0.872, despite stable allocation savings. These results do not
establish absence of regressions. In particular, the earlier Mac ARM64/.NET
12-dev results for the narrow revision included a one-item CPU regression;
these Windows results do not invalidate that observation.

The initial 72-case comparison had speed transitions during measurement in
13 cases, including unchanged controls. The first-five/last-five median ratio
exceeded 1.7 in at least one launch of each flagged case. All 13 were rerun with
the preparation above. This is consistent with insufficient warmup, but no
JIT event trace was captured to prove a cause. Initial and follow-up results
are kept separate; none of the initial observations have been dropped.

- [All 13 prewarmed follow-up cases](drift-results.md), with individual launch ratios and exploratory intervals.
- [Initial 72-case comparison](benchmark-results.md), eight passes in ABBABAAB order, 15 warmups/15 measurements/requested 200 ms.
- [Separate four-case follow-up](confirm-results.md), ABBA, 50 warmups/20 measurements/requested 500 ms.
- [Raw BDN JSON](bdn), including the two A/A calibration passes. Each JSON retains all measurements and allocation data.
- [Measured source](fixture/Workloads.cs), [semantic audit](fixture/SemanticAudit.cs), and [behavioral differences](semantics.md).
- [Reproduction instructions](REPRODUCE.md) and both original BDN driver variants in [reproduce](reproduce).

The A/A calibration launch means differed by +5.5%, -2.3%, +0.8%, and +8.5%
across its four cases. Small timing differences deserve caution. The follow-up
intervals use only two independent pairs and are correspondingly imprecise.

## Validation

The Release/net10.0 compiler build with analyzers passed with zero warnings and
errors. Collection-expression tests: 2,037 passed, 7 skipped, 0 failures.
Whitespace verification and git diff --check passed. Both compiled 72-case
fixtures pass full ordered-value checks, saved under fixture/A and fixture/B.

The local full Emit3 run had 28,019 passes, 49 skips and two failures in
KeyContainerSigningTempPathMissing. Both were Japanese/English diagnostic-text
mismatches and reproduced after substituting the unmodified baseline compiler.

[Upstream roslyn-CI passed on the published head](https://dev.azure.com/dnceng-public/cbb18261-c48f-4abb-8651-8cdcb5474649/_build/results?buildId=1620774).
The integration pipeline was skipped by its path filter.
