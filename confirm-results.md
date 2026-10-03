# confirm: published PR versus centralized revision

A = published PR head 307e044; B = local revision. Mean ns/op averages the two launches per compiler. Paired ratio is the geometric mean of B1/A1 and B2/A2, not the quotient of the pooled means. Ratios below 1 favor B. All cases retained.

| Case | A ns/op | B ns/op | Paired geometric B/A | Pair ratios | A B/op | B B/op |
| --- | ---: | ---: | ---: | --- | ---: | ---: |
| enumerable-where-1 | 24.54 | 22.63 | 0.924 | 0.993, 0.860 | 144.00 | 136.00 |
| mutable-control-1 | 18.62 | 18.51 | 0.994 | 1.028, 0.961 | 112.00 | 112.00 |
| mutable-control-100000 | 164239.14 | 181677.42 | 1.099 | 1.229, 0.982 | 400188.00 | 400188.00 |
| rolist-where-1 | 22.46 | 20.86 | 0.929 | 0.933, 0.924 | 144.00 | 136.00 |
| rolist-where-100000 | 372807.61 | 157197.43 | 0.422 | 0.405, 0.440 | 1049143.00 | 400211.00 |
