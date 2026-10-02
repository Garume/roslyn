// Compiler-pair BDN workloads. No timing is performed in this assembly.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

public static class Program
{
    private static int[] s_values = Array.Empty<int>();
    private static string[] s_strings = Array.Empty<string>();
    private static List<int> s_list = new List<int>();
    private static IEnumerable<int> s_saved = Array.Empty<int>();
    private static readonly int[] s_prefix = { -4, -3, -2, -1 };
    private static readonly Dictionary<short, OpCode> s_opcodes = typeof(OpCodes).GetFields()
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(op => op.Value);

    private sealed record Case(string Id, int Size, Func<object> Operation, int ExpectedCount);
    private sealed record Sample(double BytesPerOperation, double NanosecondsPerOperation, int Capacity);

    public static int Main(string[] args)
    {
        var cases = Cases();
        if (args.SequenceEqual(new[] { "--list" }))
            Console.WriteLine(JsonSerializer.Serialize(cases.Select(c => c.Id)));
        else if (args.SequenceEqual(new[] { "--il" }))
            Console.WriteLine(JsonSerializer.Serialize(typeof(Program).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name.StartsWith("Case", StringComparison.Ordinal)).ToDictionary(m => m.Name, Calls)));
        else if (args.SequenceEqual(new[] { "--verify" }))
        {
            foreach (var c in cases)
            {
                Setup(c.Size);
                object result = c.Operation();
                Check(result, c.ExpectedCount);
                if (c.Id.StartsWith("reference-"))
                {
                    if (!((IEnumerable<string>)result).SequenceEqual(s_strings)) throw new Exception(c.Id);
                    continue;
                }
                IEnumerable<int> expected = s_values;
                if (c.Id.StartsWith("false-")) expected = Array.Empty<int>();
                else if (c.Id.StartsWith("half-")) expected = s_values.Where(x => (x & 1) == 0);
                else if (c.Id.StartsWith("front16-")) expected = s_values.Take(16);
                else if (c.Id.StartsWith("existing-")) expected = s_prefix.Concat(s_values);
                else if (c.Id.StartsWith("expanded-skip-")) expected = s_values.Skip(c.Size / 4).Take(c.Size / 2);
                else if (c.Id.StartsWith("expanded-") && !c.Id.StartsWith("expanded-concat-")) expected = s_values.Select(x => x + 1);
                if (!((IEnumerable<int>)result).SequenceEqual(expected)) throw new Exception(c.Id);
            }
            Console.WriteLine(JsonSerializer.Serialize(new { passed = cases.Length }));
        }
        else throw new ArgumentException("Only --list, --il and --verify are supported.");
        return 0;
    }

    private static void Setup(int size)
    {
        s_values = new int[size];
        for (int i = 0; i < size; i++) s_values[i] = i;
        s_list = new List<int>(s_values);
        s_saved = s_values.Where(static x => x >= 0).Select(static x => x + 1);
        s_strings = new string[size];
        Array.Fill(s_strings, "prepared-reference");
    }

    private static void Check(object value, int expected)
    {
        if (((ICollection)value).Count != expected) throw new InvalidOperationException("Result count mismatch");
        GC.KeepAlive(value);
    }

    private static Case[] Cases()
    {
        var cases = new List<Case>();
        foreach (int size in new[] { 100_000, 1_000_000 })
        {
            cases.Add(new Case("main-ilist-" + size, size, CaseIList, size));
            cases.Add(new Case("main-icollection-" + size, size, CaseICollection, size));
            cases.Add(new Case("main-list-" + size, size, CaseList, size));
            cases.Add(new Case("main-tolist-" + size, size, CaseToList, size));
            cases.Add(new Case("main-addrange-" + size, size, CaseAddRange, size));
        }
        cases.Add(new Case("false-ilist", 100_000, CaseFalseIList, 0));
        cases.Add(new Case("false-icollection", 100_000, CaseFalseICollection, 0));
        cases.Add(new Case("half-ilist", 100_000, CaseHalfIList, 50_000));
        cases.Add(new Case("half-icollection", 100_000, CaseHalfICollection, 50_000));
        cases.Add(new Case("front16-ilist", 100_000, CaseFrontIList, 16));
        cases.Add(new Case("front16-icollection", 100_000, CaseFrontICollection, 16));
        foreach (int size in new[] { 0, 1, 8, 9, 17 })
        {
            cases.Add(new Case("small-ilist-" + size, size, CaseIList, size));
            cases.Add(new Case("small-icollection-" + size, size, CaseICollection, size));
        }
        cases.Add(new Case("array-ilist", 100_000, CaseArrayIList, 100_000));
        cases.Add(new Case("array-icollection", 100_000, CaseArrayICollection, 100_000));
        cases.Add(new Case("reference-ilist", 100_000, CaseReferenceIList, 100_000));
        cases.Add(new Case("reference-icollection", 100_000, CaseReferenceICollection, 100_000));
        cases.Add(new Case("yield-ilist", 100_000, CaseYieldIList, 100_000));
        cases.Add(new Case("yield-icollection", 100_000, CaseYieldICollection, 100_000));
        cases.Add(new Case("listwhere-ilist", 100_000, CaseListWhereIList, 100_000));
        cases.Add(new Case("listwhere-icollection", 100_000, CaseListWhereICollection, 100_000));
        cases.Add(new Case("existing-addrange-grow", 100_000, CaseExistingGrow, 100_004));
        cases.Add(new Case("existing-addrange-reserved", 100_000, CaseExistingReserved, 100_004));
        foreach (int size in new[] { 1, 32, 100_000 })
        {
            cases.Add(new Case("expanded-arraychain-ilist-" + size, size, CaseExpandedArraychainIList, size));
            cases.Add(new Case("expanded-arraychain-icollection-" + size, size, CaseExpandedArraychainICollection, size));
            cases.Add(new Case("expanded-listchain-ilist-" + size, size, CaseExpandedListchainIList, size));
            cases.Add(new Case("expanded-listchain-icollection-" + size, size, CaseExpandedListchainICollection, size));
            cases.Add(new Case("expanded-select-ilist-" + size, size, CaseExpandedSelectIList, size));
            cases.Add(new Case("expanded-select-icollection-" + size, size, CaseExpandedSelectICollection, size));
            cases.Add(new Case("expanded-saved-ilist-" + size, size, CaseExpandedSavedIList, size));
            cases.Add(new Case("expanded-saved-icollection-" + size, size, CaseExpandedSavedICollection, size));
            cases.Add(new Case("expanded-skip-ilist-" + size, size, CaseExpandedSkipIList, size / 2));
            cases.Add(new Case("expanded-skip-icollection-" + size, size, CaseExpandedSkipICollection, size / 2));
            cases.Add(new Case("expanded-concat-ilist-" + size, size, CaseExpandedConcatIList, size));
            cases.Add(new Case("expanded-concat-icollection-" + size, size, CaseExpandedConcatICollection, size));
        }
        if (cases.Count != 72) throw new InvalidOperationException("Unexpected matrix size");
        return cases.ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseIList() => [.. s_values.Where(static x => true)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseICollection() => [.. s_values.Where(static x => true)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static List<int> CaseList() => [.. s_values.Where(static x => true)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static List<int> CaseToList() => s_values.Where(static x => true).ToList();
    [MethodImpl(MethodImplOptions.NoInlining)] public static List<int> CaseAddRange() { var result = new List<int>(); result.AddRange(s_values.Where(static x => true)); return result; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseFalseIList() => [.. s_values.Where(static x => false)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseFalseICollection() => [.. s_values.Where(static x => false)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseHalfIList() => [.. s_values.Where(static x => (x & 1) == 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseHalfICollection() => [.. s_values.Where(static x => (x & 1) == 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseFrontIList() => [.. s_values.Where(static x => x < 16)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseFrontICollection() => [.. s_values.Where(static x => x < 16)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseArrayIList() => [.. s_values];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseArrayICollection() => [.. s_values];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<string> CaseReferenceIList() => [.. s_strings.Where(static x => x.Length != 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<string> CaseReferenceICollection() => [.. s_strings.Where(static x => x.Length != 0)];
    private static IEnumerable<int> Yield() { foreach (int value in s_values) yield return value; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseYieldIList() => [.. Yield()];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseYieldICollection() => [.. Yield()];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseListWhereIList() => [.. s_list.Where(static x => true)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseListWhereICollection() => [.. s_list.Where(static x => true)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static List<int> CaseExistingGrow() { var result = new List<int>(s_prefix); result.AddRange(s_values.Where(static x => true)); return result; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static List<int> CaseExistingReserved() { var result = new List<int>(s_values.Length + 4); result.AddRange(s_prefix); result.AddRange(s_values.Where(static x => true)); return result; }

    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseExpandedArraychainIList() => [.. s_values.Where(static x => x >= 0).Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseExpandedArraychainICollection() => [.. s_values.Where(static x => x >= 0).Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseExpandedListchainIList() => [.. s_list.Where(static x => x >= 0).Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseExpandedListchainICollection() => [.. s_list.Where(static x => x >= 0).Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseExpandedSelectIList() => [.. s_values.Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseExpandedSelectICollection() => [.. s_values.Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseExpandedSavedIList() => [.. s_saved];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseExpandedSavedICollection() => [.. s_saved];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseExpandedSkipIList() => [.. s_values.Skip(s_values.Length / 4).Take(s_values.Length / 2)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseExpandedSkipICollection() => [.. s_values.Skip(s_values.Length / 4).Take(s_values.Length / 2)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> CaseExpandedConcatIList() => [.. s_values.Take(s_values.Length / 2).Concat(s_values.Skip(s_values.Length / 2))];
    [MethodImpl(MethodImplOptions.NoInlining)] public static ICollection<int> CaseExpandedConcatICollection() => [.. s_values.Take(s_values.Length / 2).Concat(s_values.Skip(s_values.Length / 2))];

    private static string[] Calls(MethodInfo method)
    {
        byte[] il = method.GetMethodBody()!.GetILAsByteArray()!;
        var calls = new List<string>();
        for (int offset = 0; offset < il.Length;)
        {
            byte first = il[offset++];
            short key = first == 0xfe ? (short)(0xfe00 | il[offset++]) : first;
            OpCode op = s_opcodes[key];
            if (op.OperandType == OperandType.InlineMethod)
            {
                MethodBase target = method.Module.ResolveMethod(BitConverter.ToInt32(il, offset))!;
                calls.Add(target.DeclaringType + "::" + target.Name);
            }
            offset += op.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                _ => 4
            };
        }
        return calls.ToArray();
    }
}
