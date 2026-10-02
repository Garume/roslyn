# confirm follow-up

50 warmups, 20 measured iterations, requested 500 ms each.

ABBA, two independent launches per compiler. Same CPU affinity and runtime as the initial comparison. Retain all observations. Ratios below 1 favor B; intervals with only two pairs are necessarily imprecise.

| Case | A ns/op | B ns/op | B/A | Pair ratios | 95% interval | First/last medians in each launch |
| --- | ---: | ---: | ---: | --- | --- | --- |
| main-addrange-1000000 | 2430145.39 | 2359962.27 | 0.971 | 0.976, 0.966 | 0.907–1.040 | 1.005, 0.960, 1.070, 1.016 |
| main-tolist-1000000 | 997720.50 | 955831.17 | 0.959 | 0.901, 1.021 | 0.433–2.123 | 0.873, 1.024, 1.005, 1.010 |
| small-icollection-8 | 27.63 | 14.59 | 0.528 | 0.514, 0.543 | 0.373–0.747 | 0.999, 0.986, 0.998, 1.007 |
| small-ilist-8 | 28.00 | 16.17 | 0.575 | 0.529, 0.625 | 0.199–1.662 | 0.989, 0.986, 1.070, 0.982 |
