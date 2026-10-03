# Reproduction

Use Windows x64 with SDK 11.0.100-rc.1.26425.128, runtime 10.0.10, and reference pack 10.0.1. The checked-in driver uses BDN 0.15.8. Serialize compilation and measurement; keep unrelated workloads idle. Original measurements used a shared workstation, so their timing limitations still apply.

Build the C# compiler in separate Roslyn checkouts at A (`307e04425fc59844bedc16bed3a8b9ab9ba7b40a`) and measured B (`f4d3fb7e36042c8fb3b4466000749b353b67a2e2`). Follow Roslyn's SDK/bootstrap instructions first. In each checkout:

```powershell
.\.dotnet\dotnet.exe build src/Compilers/CSharp/csc/AnyCpu/csc.csproj `
  -c Release -f net10.0 -m:1 -nr:false `
  -p:RunAnalyzersDuringBuild=true -p:BuildInParallel=false `
  -p:UseSharedCompilation=false -p:RestoreDisableParallel=true
```

Save `artifacts/bin/csc/Release/net10.0` separately for A and B. The experiment copied A's compiler directory to B and replaced only `Microsoft.CodeAnalysis.CSharp.dll` with B's build output. The manifest records hashes. Rebuilding can change compiler build metadata. The final correction is `2278990af3a56b2cd72baf94c1df171150dd4acd`; its workload and audit outputs match measured B byte-for-byte, as recorded in `final-correction/fixture-comparison.json`.

Run the following in the evidence directory, setting installation paths:

```powershell
$benchDotnet = 'C:/path/to/dotnet.exe'
$benchRefs = 'C:/path/to/Microsoft.NETCore.App.Ref/10.0.1/ref/net10.0'
$benchCompilers = @{ A = 'C:/path/to/compiler-A'; B = 'C:/path/to/compiler-B' }
$benchRoot = (Get-Location).Path
foreach ($variant in 'A', 'B') {
    $output = Join-Path $benchRoot "compiled/$variant"
    New-Item -ItemType Directory -Force $output | Out-Null
    foreach ($sourceName in 'Workloads', 'SemanticAudit') {
        $assembly = Join-Path $output "$sourceName.dll"
        $arguments = @('/nostdlib+', '/target:exe', '/optimize+', '/langversion:preview',
            '/nullable:enable', '/deterministic+', "/out:`"$assembly`"")
        $arguments += Get-ChildItem $benchRefs -Filter *.dll | Sort-Object Name |
            ForEach-Object { '/reference:"' + $_.FullName + '"' }
        $arguments += '"' + (Join-Path $benchRoot "fixture/$sourceName.cs") + '"'
        $response = Join-Path $output "$sourceName.rsp"
        $arguments | Set-Content -Encoding utf8 $response
        & $benchDotnet (Join-Path $benchCompilers[$variant] 'csc.dll') /noconfig "@$response"
        if ($LASTEXITCODE) { throw 'Fixture compilation failed' }
        '{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.10"},"rollForward":"Disable"}}' |
            Set-Content -Encoding utf8 (Join-Path $output "$sourceName.runtimeconfig.json")
    }
    & $benchDotnet (Join-Path $output 'Workloads.dll') --verify
    if ($LASTEXITCODE) { throw 'Result verification failed' }
    & $benchDotnet (Join-Path $output 'Workloads.dll') --il
    & $benchDotnet (Join-Path $output 'SemanticAudit.dll')
}

Set-Location (Join-Path $benchRoot 'driver')
& $benchDotnet build CpuBench.csproj -c Release --locked-mode
if ($LASTEXITCODE) { throw 'Driver build failed' }
$env:DOTNET_ROOT = Split-Path $benchDotnet
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:PERFLAB_PREWARM_SECONDS = '5'
$env:PERFLAB_CPU_CASES = Get-Content (Join-Path $benchRoot 'cases.json') -Raw
$pass = 0
foreach ($variant in 'A', 'B', 'B', 'A') {
    $env:PERFLAB_CPU_ASSEMBLY = Join-Path $benchRoot "compiled/$variant/Workloads.dll"
    & $benchDotnet ./bin/Release/net10.0/CpuBench.dll --filter '*' --cli $benchDotnet `
      --launchCount 1 --warmupCount 15 --iterationCount 15 --iterationTime 200 `
      --outliers DontRemove --join --artifacts "$benchRoot/new-results/pass-$pass-$variant"
    if ($LASTEXITCODE) { throw 'Benchmark failed' }
    $pass++
}
```

Use a dotnet installation whose .NET 10 runtime is 10.0.10 and verify that the child-process header reports that version. The driver fixes CPU affinity to logical CPU 0. Setup loads the precompiled workload and checks its result; timed calls execute one operation through the same delegate wrapper for A and B.

For confirmation, loop through `confirm-cases.json`. For each case, set `PERFLAB_CPU_CASES` to a JSON array containing only that case, run A/B/B/A consecutively, and change `--iterationTime` to 500. Give each run a distinct artifact directory. The five cases include both readonly cases and unchanged controls. Keep all initial and confirmation results separate.

Raw BDN exports contain each launch's mean and allocation count. Tables average the two launch means per compiler. Paired geometric ratios combine B1/A1 and B2/A2; they are not the ratio of the pooled means. The exploratory intervals in comparison JSON use two launch pairs and are not adjusted for multiple comparisons. Individual iterations are not independent launches.

`recorded-scripts/` preserves the scripts used for the original measurements, including their original workspace paths. Adapt those paths if using the scripts directly. The commands above use explicit paths for reproduction elsewhere.
