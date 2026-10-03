using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
public static class Program
{
    private static readonly Dictionary<short, OpCode> s_opcodes = typeof(OpCodes).GetFields()
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!).ToDictionary(op => op.Value);
    private static int[] s_values = [];
    private sealed record Case(string Id, int Size, Func<object> Operation, int ExpectedCount);
    private static void Setup(int size) => s_values = Enumerable.Range(0, size).ToArray();
    private static IEnumerable<int> Values() { foreach (int value in s_values) yield return value; }
    private static Case[] Cases() => [
        new("enumerable-where-1", 1, enumerable_where, 1),
        new("enumerable-where-8", 8, enumerable_where, 8),
        new("enumerable-where-100000", 100000, enumerable_where, 100000),
        new("enumerable-chain-100000", 100000, enumerable_chain, 100000),
        new("enumerable-yield-1", 1, enumerable_yield, 1),
        new("enumerable-yield-100000", 100000, enumerable_yield, 100000),
        new("rocollection-where-1", 1, rocollection_where, 1),
        new("rocollection-where-8", 8, rocollection_where, 8),
        new("rocollection-where-100000", 100000, rocollection_where, 100000),
        new("rocollection-chain-100000", 100000, rocollection_chain, 100000),
        new("rocollection-yield-1", 1, rocollection_yield, 1),
        new("rocollection-yield-100000", 100000, rocollection_yield, 100000),
        new("rolist-where-1", 1, rolist_where, 1),
        new("rolist-where-8", 8, rolist_where, 8),
        new("rolist-where-100000", 100000, rolist_where, 100000),
        new("rolist-chain-100000", 100000, rolist_chain, 100000),
        new("rolist-yield-1", 1, rolist_yield, 1),
        new("rolist-yield-100000", 100000, rolist_yield, 100000),
        new("mutable-control-1", 1, mutable, 1),
        new("mutable-control-100000", 100000, mutable, 100000),
        new("list-control-1", 1, list, 1),
        new("list-control-100000", 100000, list, 100000),
        new("rolist-none-100000", 100000, none, 0),
        new("rolist-half-100000", 100000, half, 50000),
        new("known-control-100000", 100000, known, 100000)
    ];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IEnumerable<int> enumerable_where() => [.. s_values.Where(static x => x >= 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IEnumerable<int> enumerable_chain() => [.. s_values.Where(static x => x >= 0).Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IEnumerable<int> enumerable_yield() => [.. Values()];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyCollection<int> rocollection_where() => [.. s_values.Where(static x => x >= 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyCollection<int> rocollection_chain() => [.. s_values.Where(static x => x >= 0).Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyCollection<int> rocollection_yield() => [.. Values()];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyList<int> rolist_where() => [.. s_values.Where(static x => x >= 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyList<int> rolist_chain() => [.. s_values.Where(static x => x >= 0).Select(static x => x + 1)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyList<int> rolist_yield() => [.. Values()];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IList<int> mutable() => [.. s_values.Where(static x => x >= 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static List<int> list() => [.. s_values.Where(static x => x >= 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyList<int> none() => [.. s_values.Where(static x => x < 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyList<int> half() => [.. s_values.Where(static x => (x & 1) == 0)];
    [MethodImpl(MethodImplOptions.NoInlining)] public static IReadOnlyList<int> known() => [.. s_values];
    public static void Main(string[] args)
    {
        if (args[0] == "--list") { Console.WriteLine(JsonSerializer.Serialize(Cases().Select(c => c.Id))); return; }
        if (args[0] == "--il")
        {
            Console.WriteLine(JsonSerializer.Serialize(typeof(Program).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name != "Main").ToDictionary(m => m.Name, m => new { size = m.GetMethodBody()!.GetILAsByteArray()!.Length, calls = Calls(m) })));
            return;
        }
        foreach (var c in Cases())
        {
            Setup(c.Size);
            var result = (IEnumerable<int>)c.Operation();
            IEnumerable<int> expected = s_values;
            if (c.Id.Contains("-chain-")) expected = expected.Select(x => x + 1);
            if (c.Id.Contains("-none-")) expected = [];
            if (c.Id.Contains("-half-")) expected = expected.Where(x => (x & 1) == 0);
            if (!result.SequenceEqual(expected) || ((ICollection)result).Count != c.ExpectedCount) throw new Exception(c.Id);
            if (!c.Id.StartsWith("mutable-") && !c.Id.StartsWith("list-"))
            {
                if (result is List<int> || !((IList<int>)result).IsReadOnly) throw new Exception(c.Id + " wrapper");
                try { ((IList<int>)result).Add(99); throw new Exception(c.Id + " mutable"); }
                catch (NotSupportedException) { }
            }
        }
        Console.WriteLine(JsonSerializer.Serialize(new { passed = Cases().Length }));
    }
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
