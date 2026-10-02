# Reproduction

The results were collected on Windows x64. The commands below use PowerShell.
Use a dotnet installation with Microsoft.NETCore.App 10.0.10 and the driver SDK
11.0.100-rc.1.26425.128, plus the net10.0 reference pack 10.0.1. Keep the machine
idle and serialize all builds, tests and benchmark launches.

Build Roslyn in two worktrees at baseline
`8ddc2f18d7a23028823d0870abf60e4b61cda24e` and candidate
`307e04425fc59844bedc16bed3a8b9ab9ba7b40a`. In each checkout:

```powershell
.\.dotnet\dotnet.exe build src/Compilers/CSharp/csc/AnyCpu/csc.csproj `
  -c Release -f net10.0 -m:1 -nr:false `
  -p:RunAnalyzersDuringBuild=true -p:BuildInParallel=false `
  -p:UseSharedCompilation=false -p:RestoreDisableParallel=true `
  -p:RestoreUseStaticGraphEvaluation=false
```

Save the compiler output directories separately. The original experiment kept
the compiler host and Microsoft.CodeAnalysis.dll identical and substituted only
Microsoft.CodeAnalysis.CSharp.dll for B. Check hashes against manifest.json if
using the original binaries; a rebuild can differ in build metadata.

Run from this evidence directory and set these paths to your installations:

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
    & $benchDotnet (Join-Path $output 'SemanticAudit.dll')
}
```

Set up a separate driver directory. Copy BdnDriverPrewarm.cs there as Program.cs,
and copy Driver.csproj and packages.lock.json from reproduce/. Build in Release.
Pin its runtime with Driver.runtimeconfig.json after the build:

```powershell
& $benchDotnet build ./driver/Driver.csproj -c Release --locked-mode
if ($LASTEXITCODE) { throw 'Driver build failed' }
'{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.10"},"rollForward":"Disable"}}' |
    Set-Content -Encoding utf8 ./driver/bin/Release/net10.0/Driver.runtimeconfig.json
```

For the 13-case follow-up, set the cases and run one variant at a time. Use
distinct artifact paths for A, B, B, A. Run from the driver directory so BDN can
find Driver.csproj. The driver fixes affinity to logical CPU 0.

```powershell
$env:PERFLAB_CPU_CASES = (Get-Content "$benchRoot/drift-cases.json" -Raw |
    ConvertFrom-Json).PSObject.Properties.Name | ConvertTo-Json -Compress
$env:PERFLAB_PREWARM_SECONDS = '5'
$env:PERFLAB_CPU_ASSEMBLY = Join-Path $benchRoot 'compiled/A/Workloads.dll'
& $benchDotnet ./bin/Release/net10.0/Driver.dll --filter '*' --cli $benchDotnet `
  --launchCount 1 --warmupCount 15 --iterationCount 15 --iterationTime 200 `
  --outliers DontRemove --join --artifacts "$benchRoot/new-results/drift-0-A"
if ($LASTEXITCODE) { throw 'Benchmark failed' }
```

For the initial 72-case protocol, use BdnDriverOriginal.cs and get the case list
from `Workloads.dll --list`. Run eight passes in ABBABAAB order with the same
15/15/200 options. For the separate confirmation use cases small-ilist-8,
small-icollection-8, main-addrange-1000000 and main-tolist-1000000, in ABBA order,
with 50 warmups, 20 measurements and iterationTime 500. It used the original
driver without explicit prewarming.

The raw JSON files contain each launch's Statistics.Mean, OriginalValues and
Memory.BytesAllocatedPerOperation. The short table averages launch means for
each variant. Full follow-up reports also give geometric means of paired B/A
ratios and t intervals on their logs, with two independent pairs (df=1).
They are exploratory intervals without multiple-comparison adjustment. Do not
pool individual iterations as independent launches or combine the different
warmup protocols into a single result.
