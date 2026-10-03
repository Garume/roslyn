using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

class Program
{
    static readonly List<string> events = new();
    static object previous = new List<int> { 9 };
    static IList<int> Interface(IEnumerable<int> input) => [.. input];
    static ICollection<int> Collection(IEnumerable<int> input) => [.. input];
    static IEnumerable<int> Enumerable(IEnumerable<int> input) => [.. input];
    static IReadOnlyCollection<int> ReadOnlyCollection(IEnumerable<int> input) => [.. input];
    static IReadOnlyList<int> ReadOnlyList(IEnumerable<int> input) => [.. input];
    static List<int> Concrete(IEnumerable<int> input) => [.. input];
    static IList<int> ArrayInterface(int[] input) => [.. input];
    static List<int> ArrayConcrete(int[] input) => [.. input];

    static void Main()
    {
        var observations = new List<object>();
        foreach (string scenario in new[] { "normal", "mutating-list", "mutating-array", "null-enumerable", "get", "move", "current", "predicate" })
        foreach (string target in new[] { "interface", "collection", "enumerable", "readonly-collection", "readonly-list", "concrete" })
        {
            events.Clear(); previous = new List<int> { 9 };
            string? error = null;
            try
            {
                IEnumerable<int> input;
                if (scenario == "mutating-list")
                {
                    var list = new List<int> { 1, 2 };
                    input = list.Where(x => { events.Add("p:" + x); if (x == 1) list.Add(3); return true; });
                }
                else if (scenario == "mutating-array")
                {
                    var array = new[] { 1, 2 };
                    input = array.Where(x => { events.Add("p:" + x); if (x == 1) array[1] = 3; return true; });
                }
                else if (scenario == "null-enumerable") input = null!;
                else if (scenario == "predicate") input = new[] { 1, 2 }.Where(x => { events.Add("p:" + x); if (x == 2) throw new Exception("predicate"); return true; });
                else input = new Probe(scenario);
                previous = target switch { "interface" => Interface(input), "collection" => Collection(input), "enumerable" => Enumerable(input), "readonly-collection" => ReadOnlyCollection(input), "readonly-list" => ReadOnlyList(input), _ => Concrete(input) };
            }
            catch (Exception e) { error = e.GetType().Name + ":" + (e is ArgumentException a ? a.ParamName : e.Message); }
            observations.Add(new { scenario, target, error, events = events.ToArray(), result = ((IEnumerable<int>)previous).ToArray() });
        }
        foreach (string target in new[] { "interface", "concrete" })
        {
            string? error = null;
            try { previous = target == "interface" ? ArrayInterface(null!) : ArrayConcrete(null!); }
            catch (Exception e) { error = e.GetType().Name + ":" + (e is ArgumentException a ? a.ParamName : e.Message); }
            observations.Add(new { scenario = "null-array", target, error });
        }
        Console.WriteLine(JsonSerializer.Serialize(observations));
    }

    sealed class Probe(string failure) : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            events.Add("get");
            if (failure == "get") throw new Exception("get");
            return new Cursor(failure);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    sealed class Cursor(string failure) : IEnumerator<int>
    {
        int position;
        public bool MoveNext()
        {
            events.Add("move:" + ++position);
            if (failure == "move" && position == 2) throw new Exception("move");
            return position <= 2;
        }
        public int Current
        {
            get
            {
                events.Add("current:" + position);
                if (failure == "current" && position == 2) throw new Exception("current");
                return position;
            }
        }
        object IEnumerator.Current => Current;
        public void Reset() => throw new NotSupportedException();
        public void Dispose() => events.Add("dispose");
    }
}
