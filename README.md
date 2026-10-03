# Roslyn #85777: readonly single-spread lowering

Supporting data for https://github.com/dotnet/roslyn/pull/85777.

## Revisions

- A: preceding PR commit `307e04425fc59844bedc16bed3a8b9ab9ba7b40a`.
- Measured B: `f4d3fb7e36042c8fb3b4466000749b353b67a2e2`, the shared-helper revision. `candidate.patch` and `manifest.json` record the measured source and hashes.
- Final correction: `2278990af3a56b2cd72baf94c1df171150dd4acd` restores the early concrete List check to avoid constructing unused bound nodes, while retaining the shared helper for interfaces. The measured workload and semantic-audit assemblies rebuilt with this correction are byte-for-byte identical to measured B; see [comparison](final-correction/fixture-comparison.json).

This comparison isolates the readonly extension. The earlier upstream-main versus mutable-interface comparison remains [separate](https://github.com/Garume/roslyn/tree/f98ea4858a27f599c531ee15de51a179d921a040).

## Results and interpretation

[Initial 25-case table](pass-results.md), [five-case confirmation](confirm-results.md), and [raw BDN JSON](bdn/) retain all observations. The initial run had substantial temporal drift in unchanged controls. Confirmation interleaved A/B/B/A within each case, including large and small unchanged controls. The large readonly Where case was faster in both pairs (mean 372.81 to 157.20 us), with allocation changing from 1,049,143 to 400,211 B/op. One large unchanged-control launch remained slower; precise small-input speedup claims are not justified. The slight initial one-item IEnumerable slowdown did not recur in confirmation.

Windows 11 x64, Ryzen AI 9 HX 370, .NET 10.0.10, BDN 0.15.8, MemoryDiagnoser, affinity mask 1. Each launch uses five seconds of execution before normal warmup, 15 warmup and 15 measurement iterations, requested duration 200 ms initially and 500 ms in confirmation. Outliers retained. This is a shared workstation and warm steady-state experiment. It does not establish performance on other runtimes/architectures or measure compiler throughput.

## Validation

Final correction: compiler and Emit3 Release/net10.0 builds with analyzers passed with zero warnings/errors. Collection-expression tests: 2,058 passed, 7 skipped, 0 failed. [TRX and command receipts](validation/) are included. No full-suite rerun or final-head CI success is claimed here.

The 25 workload cases passed ordered-value and readonly-wrapper checks for both measured compilers and again with the final correction. The semantic audit records 50 observations per compiler. For each readonly target, source-list mutation and null-enumerable exception parameter differences match the existing concrete List behavior; see [all changes](semantic-comparison.json) and [source](fixture/SemanticAudit.cs). The known-length readonly array path is unchanged.

## Reproduction and provenance

[Instructions](REPRODUCE.md), [workloads](fixture/Workloads.cs), [driver](driver/Program.cs), [hashes](manifest.json), and [environment](environment.json). The recorded scripts preserve the original workflow and include local paths; the instructions explain how to supply your own paths. Compiler binaries are not uploaded.

Table labels such as "published PR head" and "local revision" refer to A and B at measurement time, as identified above.
