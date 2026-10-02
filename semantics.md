# Semantic audit

Measured on Windows x64, Microsoft.NETCore.App 10.0.10, using the same source and
reference assemblies with the saved A/B compilers. Raw observations and source
are under `fixture/`. This is evidence for review, not a claim that all possible
user enumerables are equivalent.

All 26 candidate observations agree with the existing concrete `List<int>`
lowering for the corresponding scenario. Normal enumeration, source-array
element mutation, GetEnumerator/MoveNext/Current failures, and predicate failure
preserve the observed event order and result/exception. Disposal happens on
enumeration failures, and the destination retains its old value when construction
throws.

There are five A/B observation differences:

| Source | Previous interface lowering | Candidate interface lowering |
| --- | --- | --- |
| `list.Where(predicate)` whose first predicate invocation appends to the source list, IList and ICollection | InvalidOperationException after first predicate; destination stays `[9]` | Predicates see original two elements; succeeds with `[1, 2]` |
| Null `IEnumerable<int>`, IList and ICollection | ArgumentNullException, ParamName `collection` | ArgumentNullException, ParamName `source` |
| Null `int[]`, IList | NullReferenceException | ArgumentNullException, ParamName `source` |

These are observable compatibility differences. Reusing an existing helper does
not erase them. They are also reported in the PR description and update comment.

The C# collection-expression proposal permits implementation optimizations and
assumes well-behaved collections, but also constrains observable differences.
It does not by itself constitute a maintainer approval of every exception or
mutation difference listed above:
https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/collection-expressions.md#spec-clarifications

The requested implementation direction is the general reuse explicitly described
by RikkiGibson in https://github.com/dotnet/roslyn/pull/85777#issuecomment-5923369186.
The candidate remains subject to maintainer review; publishing the revision does
not mean it is ready to merge without that review.
