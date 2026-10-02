using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;

var summaries = BenchmarkSwitcher.FromAssembly(typeof(CompilerBench).Assembly)
    .Run(args, DefaultConfig.Instance.AddExporter(JsonExporter.Full).AddJob(Job.Default.WithAffinity(new IntPtr(1)).AsMutator())).ToArray();
return summaries.Length == 0 || summaries.Any(s => s.HasCriticalValidationErrors || s.Reports.Any(r => !r.Success)) ? 1 : 0;

[MemoryDiagnoser]
public class CompilerBench
{
    public static IEnumerable<string> Cases => JsonSerializer.Deserialize<string[]>(Environment.GetEnvironmentVariable("PERFLAB_CPU_CASES")!)!;
    [ParamsSource(nameof(Cases))] public string CaseId { get; set; } = "";
    private Func<object> operation = null!;

    [GlobalSetup]
    public void Setup()
    {
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Environment.GetEnvironmentVariable("PERFLAB_CPU_ASSEMBLY")!);
        var type = assembly.GetType("Program", throwOnError: true)!;
        // Reuse the exact case definitions and compiler-generated methods from the allocation experiment.
        var cases = (IEnumerable)type.GetMethod("Cases", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;
        object selected = cases.Cast<object>().Single(c => (string)c.GetType().GetProperty("Id")!.GetValue(c)! == CaseId);
        var caseType = selected.GetType();
        int size = (int)caseType.GetProperty("Size")!.GetValue(selected)!;
        int expected = (int)caseType.GetProperty("ExpectedCount")!.GetValue(selected)!;
        type.GetMethod("Setup", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { size });
        operation = (Func<object>)caseType.GetProperty("Operation")!.GetValue(selected)!;
        if (((ICollection)operation()).Count != expected) throw new InvalidOperationException("Case result differs from expected count");
        Console.WriteLine("// PERFLAB_COMPILER " + Environment.GetEnvironmentVariable("PERFLAB_CPU_ASSEMBLY") + " CASE " + CaseId);
    }

    // The identical delegate wrapper is present in both variants. The measured methods were compiled
    // by the saved baseline/candidate compilers, not by BenchmarkDotNet's driver compiler.
    [Benchmark] public object Run() => operation();
}
