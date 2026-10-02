# drift follow-up

5 seconds of explicit execution in GlobalSetup, then 15 warmups and 15 measured iterations, requested 200 ms each.

ABBA, two independent launches per compiler. Same CPU affinity and runtime as the initial comparison. Retain all observations. Ratios below 1 favor B; intervals with only two pairs are necessarily imprecise.

| Case | A ns/op | B ns/op | B/A | Pair ratios | 95% interval | First/last medians in each launch |
| --- | ---: | ---: | ---: | --- | --- | --- |
| existing-addrange-grow | 304271.09 | 307427.65 | 1.010 | 0.978, 1.043 | 0.673–1.517 | 0.994, 0.985, 0.994, 0.945 |
| expanded-arraychain-ilist-100000 | 313991.17 | 105018.94 | 0.334 | 0.352, 0.318 | 0.174–0.641 | 0.978, 0.952, 1.002, 1.035 |
| expanded-saved-icollection-100000 | 312311.81 | 104229.38 | 0.333 | 0.316, 0.352 | 0.168–0.660 | 0.947, 1.012, 1.000, 1.056 |
| expanded-saved-ilist-100000 | 315399.98 | 107921.09 | 0.341 | 0.361, 0.323 | 0.168–0.694 | 1.091, 0.703, 1.035, 0.945 |
| half-icollection | 296714.84 | 63669.28 | 0.215 | 0.211, 0.218 | 0.171–0.270 | 1.131, 0.990, 1.013, 1.006 |
| half-ilist | 294905.20 | 65947.94 | 0.224 | 0.234, 0.214 | 0.127–0.393 | 0.981, 1.026, 1.011, 1.177 |
| main-addrange-100000 | 304807.48 | 303453.07 | 0.996 | 1.013, 0.979 | 0.806–1.230 | 1.008, 0.994, 0.946, 0.972 |
| main-addrange-1000000 | 2302341.93 | 2374283.66 | 1.031 | 1.014, 1.048 | 0.836–1.271 | 0.989, 1.009, 1.013, 0.971 |
| main-icollection-100000 | 301332.23 | 95596.92 | 0.317 | 0.321, 0.314 | 0.277–0.364 | 0.965, 1.033, 1.005, 0.965 |
| main-ilist-100000 | 300441.25 | 97507.71 | 0.325 | 0.314, 0.336 | 0.211–0.500 | 0.982, 1.028, 0.989, 0.982 |
| reference-icollection | 568197.44 | 460867.75 | 0.811 | 0.818, 0.804 | 0.725–0.908 | 0.979, 0.991, 0.987, 0.987 |
| small-ilist-1 | 14.46 | 14.70 | 1.009 | 1.167, 0.872 | 0.159–6.403 | 1.005, 1.034, 1.012, 0.875 |
| small-ilist-8 | 27.73 | 14.95 | 0.539 | 0.542, 0.536 | 0.499–0.582 | 0.989, 0.990, 0.984, 0.996 |
