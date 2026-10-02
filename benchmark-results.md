# BDN compiler comparison

**Initial experiment:** 13 cases include launches with substantial within-run speed changes. Their timing estimates require the separate follow-up in `drift-results.md`; do not use this table alone to claim steady-state speed or absence of regressions.

Windows x64, .NET 10.0.10, BDN 0.15.8, logical CPU 0 affinity. Four independent launches per compiler in ABBABAAB order. Each launch has 15 warmups and 15 measured iterations, target 200 ms. Outliers retained. Ratios below 1 favor the candidate.

Intervals are exploratory 95% t intervals over four paired launch log ratios (not 60 independent samples). No multiple-comparison correction; small differences require confirmation. All cases, including controls and regressions, are retained.

| Case | A ns/op | B ns/op | B/A | 95% interval | A B/op | B B/op | Result |
| --- | ---: | ---: | ---: | --- | ---: | ---: | --- |
| array-icollection | 44418.97 | 45340.63 | 1.019 | 0.927–1.119 | 400129 | 400128 | inconclusive |
| array-ilist | 44263.38 | 44152.56 | 0.997 | 0.904–1.099 | 400124 | 400128 | inconclusive |
| existing-addrange-grow | 363936.53 | 369641.34 | 1.016 | 0.954–1.082 | 1.04926e+06 | 1.04923e+06 | inconclusive |
| existing-addrange-reserved | 182877.48 | 189882.41 | 1.037 | 0.924–1.162 | 400200 | 400200 | inconclusive |
| expanded-arraychain-icollection-1 | 21.70 | 20.28 | 0.934 | 0.898–0.972 | 176 | 168 | faster |
| expanded-arraychain-icollection-100000 | 398386.09 | 100264.18 | 0.252 | 0.229–0.277 | 1.04928e+06 | 400219 | faster |
| expanded-arraychain-icollection-32 | 82.61 | 59.20 | 0.703 | 0.492–1.004 | 472 | 288 | inconclusive |
| expanded-arraychain-ilist-1 | 21.19 | 20.12 | 0.951 | 0.832–1.086 | 176 | 168 | inconclusive |
| expanded-arraychain-ilist-100000 | 396987.57 | 103242.75 | 0.260 | 0.239–0.282 | 1.04934e+06 | 400231 | faster |
| expanded-arraychain-ilist-32 | 87.30 | 54.43 | 0.625 | 0.516–0.758 | 472 | 288 | faster |
| expanded-concat-icollection-1 | 11.61 | 11.19 | 0.964 | 0.914–1.017 | 120 | 120 | inconclusive |
| expanded-concat-icollection-100000 | 452340.93 | 45092.80 | 0.100 | 0.092–0.107 | 1.04943e+06 | 400260 | faster |
| expanded-concat-icollection-32 | 134.18 | 28.00 | 0.208 | 0.182–0.239 | 520 | 336 | faster |
| expanded-concat-ilist-1 | 11.73 | 11.17 | 0.953 | 0.910–0.999 | 120 | 120 | faster |
| expanded-concat-ilist-100000 | 464342.31 | 45537.62 | 0.098 | 0.084–0.114 | 1.04938e+06 | 400256 | faster |
| expanded-concat-ilist-32 | 131.27 | 26.73 | 0.203 | 0.183–0.226 | 520 | 336 | faster |
| expanded-listchain-icollection-1 | 25.15 | 22.74 | 0.911 | 0.734–1.130 | 224 | 216 | inconclusive |
| expanded-listchain-icollection-100000 | 381595.70 | 98900.95 | 0.260 | 0.231–0.292 | 1.04922e+06 | 400279 | faster |
| expanded-listchain-icollection-32 | 100.49 | 54.51 | 0.542 | 0.519–0.567 | 520 | 336 | faster |
| expanded-listchain-ilist-1 | 23.13 | 22.16 | 0.953 | 0.811–1.120 | 224 | 216 | inconclusive |
| expanded-listchain-ilist-100000 | 387774.44 | 99728.04 | 0.258 | 0.220–0.301 | 1.04928e+06 | 400280 | faster |
| expanded-listchain-ilist-32 | 100.27 | 53.60 | 0.534 | 0.501–0.570 | 520 | 336 | faster |
| expanded-saved-icollection-1 | 13.30 | 11.04 | 0.820 | 0.621–1.083 | 128 | 64 | inconclusive |
| expanded-saved-icollection-100000 | 386624.48 | 98232.93 | 0.254 | 0.246–0.263 | 1.04921e+06 | 400142 | faster |
| expanded-saved-icollection-32 | 77.20 | 43.74 | 0.567 | 0.535–0.600 | 424 | 184 | faster |
| expanded-saved-ilist-1 | 13.62 | 10.06 | 0.735 | 0.604–0.894 | 128 | 64 | faster |
| expanded-saved-ilist-100000 | 400491.96 | 104498.18 | 0.260 | 0.236–0.287 | 1.04922e+06 | 400143 | faster |
| expanded-saved-ilist-32 | 75.41 | 44.49 | 0.589 | 0.564–0.616 | 424 | 184 | faster |
| expanded-select-icollection-1 | 13.60 | 11.19 | 0.820 | 0.699–0.961 | 120 | 112 | faster |
| expanded-select-icollection-100000 | 279891.95 | 79386.39 | 0.283 | 0.244–0.328 | 1.0492e+06 | 400150 | faster |
| expanded-select-icollection-32 | 64.12 | 19.27 | 0.301 | 0.265–0.343 | 416 | 232 | faster |
| expanded-select-ilist-1 | 13.44 | 10.37 | 0.772 | 0.723–0.823 | 120 | 112 | faster |
| expanded-select-ilist-100000 | 274141.20 | 76766.29 | 0.279 | 0.255–0.306 | 1.04919e+06 | 400150 | faster |
| expanded-select-ilist-32 | 62.65 | 19.26 | 0.306 | 0.274–0.343 | 416 | 232 | faster |
| expanded-skip-icollection-1 | 6.62 | 7.37 | 1.106 | 0.904–1.352 | 80 | 80 | inconclusive |
| expanded-skip-icollection-100000 | 23072.52 | 23128.51 | 1.003 | 0.860–1.170 | 200168 | 200170 | inconclusive |
| expanded-skip-icollection-32 | 17.12 | 16.75 | 0.978 | 0.944–1.013 | 216 | 216 | inconclusive |
| expanded-skip-ilist-1 | 6.83 | 6.80 | 0.996 | 0.987–1.006 | 80 | 80 | inconclusive |
| expanded-skip-ilist-100000 | 22198.59 | 22666.14 | 1.021 | 0.970–1.074 | 200170 | 200168 | inconclusive |
| expanded-skip-ilist-32 | 17.46 | 16.43 | 0.945 | 0.782–1.143 | 216 | 216 | inconclusive |
| false-icollection | 41464.18 | 20414.69 | 0.492 | 0.482–0.503 | 80 | 80 | faster |
| false-ilist | 38550.92 | 20401.83 | 0.533 | 0.420–0.678 | 80 | 80 | faster |
| front16-icollection | 47353.17 | 21547.00 | 0.461 | 0.352–0.603 | 264 | 168 | faster |
| front16-ilist | 42109.20 | 21218.02 | 0.504 | 0.475–0.535 | 264 | 168 | faster |
| half-icollection | 289260.83 | 64543.86 | 0.223 | 0.207–0.240 | 524800 | 200136 | faster |
| half-ilist | 288305.27 | 63222.99 | 0.220 | 0.202–0.239 | 524792 | 200141 | faster |
| listwhere-icollection | 416231.52 | 103414.95 | 0.247 | 0.171–0.357 | 1.04932e+06 | 400206 | faster |
| listwhere-ilist | 424882.12 | 94438.99 | 0.224 | 0.174–0.289 | 1.0493e+06 | 400202 | faster |
| main-addrange-100000 | 379813.78 | 363643.45 | 0.958 | 0.834–1.100 | 1.04924e+06 | 1.04924e+06 | inconclusive |
| main-addrange-1000000 | 5705205.26 | 2274086.54 | 0.482 | 0.150–1.546 | 8.39009e+06 | 8.38916e+06 | inconclusive |
| main-icollection-100000 | 357039.45 | 94010.02 | 0.263 | 0.243–0.286 | 1.0492e+06 | 400177 | faster |
| main-icollection-1000000 | 2300477.44 | 919997.17 | 0.400 | 0.384–0.416 | 8.38948e+06 | 4.001e+06 | faster |
| main-ilist-100000 | 365154.14 | 94861.18 | 0.260 | 0.245–0.275 | 1.04922e+06 | 400191 | faster |
| main-ilist-1000000 | 2293892.69 | 906622.01 | 0.395 | 0.362–0.432 | 8.38977e+06 | 4.00097e+06 | faster |
| main-list-100000 | 94718.30 | 97154.50 | 1.024 | 0.888–1.182 | 400186 | 400182 | inconclusive |
| main-list-1000000 | 944127.66 | 936061.52 | 0.993 | 0.901–1.094 | 4.00114e+06 | 4.00104e+06 | inconclusive |
| main-tolist-100000 | 95181.42 | 95350.35 | 1.002 | 0.959–1.047 | 400187 | 400174 | inconclusive |
| main-tolist-1000000 | 922593.63 | 927310.30 | 1.005 | 0.897–1.126 | 4.001e+06 | 4.00109e+06 | inconclusive |
| reference-icollection | 731675.34 | 474169.07 | 0.676 | 0.412–1.111 | 2.09801e+06 | 800359 | inconclusive |
| reference-ilist | 649805.78 | 461367.20 | 0.712 | 0.633–0.800 | 2.09803e+06 | 800340 | faster |
| small-icollection-0 | 3.06 | 3.30 | 1.074 | 0.898–1.284 | 32 | 32 | inconclusive |
| small-icollection-1 | 13.96 | 12.89 | 0.920 | 0.826–1.025 | 120 | 112 | inconclusive |
| small-icollection-17 | 52.13 | 28.60 | 0.548 | 0.484–0.621 | 416 | 176 | faster |
| small-icollection-8 | 26.87 | 16.45 | 0.602 | 0.427–0.848 | 176 | 136 | faster |
| small-icollection-9 | 35.32 | 22.95 | 0.649 | 0.565–0.745 | 264 | 144 | faster |
| small-ilist-0 | 3.05 | 3.12 | 1.019 | 0.823–1.261 | 32 | 32 | inconclusive |
| small-ilist-1 | 16.61 | 13.24 | 0.809 | 0.514–1.274 | 120 | 112 | inconclusive |
| small-ilist-17 | 52.19 | 29.29 | 0.561 | 0.538–0.585 | 416 | 176 | faster |
| small-ilist-8 | 27.22 | 36.41 | 0.874 | 0.182–4.195 | 176 | 136 | inconclusive |
| small-ilist-9 | 36.05 | 22.85 | 0.635 | 0.559–0.721 | 264 | 144 | faster |
| yield-icollection | 287169.37 | 291430.38 | 1.015 | 0.941–1.094 | 1.04921e+06 | 1.04917e+06 | inconclusive |
| yield-ilist | 300324.37 | 291901.52 | 0.975 | 0.811–1.171 | 1.04918e+06 | 1.04918e+06 | inconclusive |
