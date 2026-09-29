# Persistenza del rendimento delle strategie

Sorgente: `..\..\workspaces\ftmo-pt5dav-congelato\backtests\pt5dav-tutte-2022-2026\trades.json` — 54621 trade, 218 strategie, 2022-06-01 → 2026-09-01 (giorno di uscita, UTC). Permutazioni: 100, seme 20260929. Taglio dentro/fuori campione: 2025-06-01.


## Cadenza: giorno

| L | A: Spearman medio serie storica | p (perm.) | strategie > 0 | B: IC medio | t IC | periodi IC |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | +0.005 | 0.020 | 93/218 | +0.017 | +5.23 | 1108 |
| 5 | -0.002 | 0.356 | 97/218 | +0.008 | +2.93 | 1104 |
| 20 | -0.008 | 0.871 | 93/218 | -0.001 | -0.24 | 1089 |
| 60 | -0.013 | 0.861 | 85/218 | +0.000 | +0.05 | 1049 |

**C. Rotazione** (P&L normalizzato per strategia; netto / drawdown / netto÷DD; il filtro tiene le strategie con finestra positiva, la casuale ne tiene lo stesso numero):

| L | campione | tutte | filtro | casuale (mediana netto÷DD) | percentile del filtro | quota accesa |
|---:|---|---|---|---:|---:|---:|
| 1 | tutto | 22.8 / 1.9 / 11.79 | 3.5 / 1.0 / 3.45 | 7.75 | 2% | 21% |
| 1 | dentro | 19.2 / 1.2 / 15.48 | 3.2 / 0.5 / 6.44 | 11.01 | 9% | 21% |
| 1 | fuori | 3.6 / 1.9 / 1.86 | 0.3 / 1.0 / 0.25 | 1.52 | 15% | 22% |
| 5 | tutto | 23.1 / 1.9 / 11.91 | 8.2 / 1.1 / 7.77 | 9.69 | 25% | 37% |
| 5 | dentro | 19.5 / 1.2 / 15.67 | 6.8 / 0.5 / 13.49 | 15.05 | 28% | 36% |
| 5 | fuori | 3.6 / 1.9 / 1.86 | 1.4 / 1.1 / 1.31 | 1.66 | 33% | 39% |
| 20 | tutto | 21.3 / 1.9 / 11.00 | 8.1 / 1.5 / 5.51 | 9.82 | 0% | 46% |
| 20 | dentro | 17.7 / 1.2 / 14.22 | 6.7 / 0.8 / 8.82 | 14.16 | 5% | 46% |
| 20 | fuori | 3.6 / 1.9 / 1.86 | 1.4 / 1.5 / 0.98 | 1.61 | 26% | 46% |
| 60 | tutto | 21.1 / 1.9 / 10.87 | 8.9 / 1.1 / 7.73 | 10.49 | 12% | 53% |
| 60 | dentro | 17.5 / 1.3 / 13.40 | 7.0 / 0.9 / 7.99 | 13.96 | 0% | 53% |
| 60 | fuori | 3.6 / 1.9 / 1.86 | 1.9 / 1.1 / 1.63 | 1.47 | 56% | 53% |

## Cadenza: settimana

| L | A: Spearman medio serie storica | p (perm.) | strategie > 0 | B: IC medio | t IC | periodi IC |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | -0.004 | 0.505 | 99/218 | +0.007 | +0.96 | 222 |
| 4 | -0.009 | 0.277 | 91/218 | +0.001 | +0.10 | 219 |
| 13 | -0.031 | 0.911 | 70/218 | -0.008 | -1.28 | 210 |
| 26 | -0.040 | 0.535 | 66/218 | +0.002 | +0.29 | 197 |

**C. Rotazione** (P&L normalizzato per strategia; netto / drawdown / netto÷DD; il filtro tiene le strategie con finestra positiva, la casuale ne tiene lo stesso numero):

| L | campione | tutte | filtro | casuale (mediana netto÷DD) | percentile del filtro | quota accesa |
|---:|---|---|---|---:|---:|---:|
| 1 | tutto | 10.3 / 0.8 / 13.16 | 3.7 / 0.4 / 9.49 | 9.60 | 48% | 36% |
| 1 | dentro | 8.6 / 0.5 / 17.84 | 2.4 / 0.3 / 8.14 | 16.66 | 1% | 36% |
| 1 | fuori | 1.7 / 0.8 / 2.14 | 1.3 / 0.4 / 3.35 | 1.51 | 81% | 36% |
| 4 | tutto | 9.3 / 0.8 / 11.93 | 4.0 / 0.5 / 7.67 | 10.30 | 12% | 46% |
| 4 | dentro | 7.7 / 0.5 / 15.83 | 3.3 / 0.3 / 9.93 | 15.21 | 6% | 46% |
| 4 | fuori | 1.7 / 0.8 / 2.14 | 0.7 / 0.5 / 1.30 | 1.57 | 40% | 46% |
| 13 | tutto | 9.5 / 0.8 / 12.11 | 3.9 / 0.5 / 7.68 | 10.58 | 6% | 53% |
| 13 | dentro | 7.8 / 0.5 / 15.28 | 2.9 / 0.5 / 5.71 | 15.14 | 0% | 54% |
| 13 | fuori | 1.7 / 0.8 / 2.14 | 1.0 / 0.4 / 2.26 | 1.87 | 65% | 52% |
| 26 | tutto | 8.4 / 0.8 / 10.67 | 4.5 / 0.5 / 8.31 | 9.36 | 26% | 58% |
| 26 | dentro | 6.7 / 0.5 / 12.93 | 3.6 / 0.4 / 8.84 | 13.10 | 6% | 59% |
| 26 | fuori | 1.7 / 0.8 / 2.14 | 0.9 / 0.5 / 1.72 | 1.70 | 51% | 57% |

## Cadenza: mese

| L | A: Spearman medio serie storica | p (perm.) | strategie > 0 | B: IC medio | t IC | periodi IC |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | -0.022 | 0.545 | 102/218 | -0.016 | -1.19 | 50 |
| 3 | -0.057 | 0.822 | 71/218 | -0.021 | -1.77 | 48 |
| 6 | -0.078 | 0.574 | 67/218 | -0.011 | -0.98 | 45 |
| 12 | -0.088 | 0.010 | 56/218 | +0.011 | +0.85 | 39 |

**C. Rotazione** (P&L normalizzato per strategia; netto / drawdown / netto÷DD; il filtro tiene le strategie con finestra positiva, la casuale ne tiene lo stesso numero):

| L | campione | tutte | filtro | casuale (mediana netto÷DD) | percentile del filtro | quota accesa |
|---:|---|---|---|---:|---:|---:|
| 1 | tutto | 4.8 / 0.3 / 15.23 | 2.2 / 0.2 / 11.50 | 12.51 | 43% | 46% |
| 1 | dentro | 3.9 / 0.2 / 17.30 | 1.8 / 0.1 / 12.20 | 20.15 | 7% | 46% |
| 1 | fuori | 0.9 / 0.3 / 2.77 | 0.3 / 0.2 / 1.86 | 1.85 | 51% | 44% |
| 3 | tutto | 4.7 / 0.3 / 14.96 | 2.4 / 0.2 / 11.36 | 12.23 | 41% | 54% |
| 3 | dentro | 3.9 / 0.2 / 16.12 | 1.9 / 0.2 / 9.94 | 16.93 | 1% | 54% |
| 3 | fuori | 0.9 / 0.3 / 2.77 | 0.5 / 0.2 / 2.57 | 2.02 | 70% | 54% |
| 6 | tutto | 4.1 / 0.3 / 12.86 | 2.3 / 0.2 / 10.83 | 10.91 | 48% | 59% |
| 6 | dentro | 3.2 / 0.2 / 13.20 | 1.9 / 0.2 / 11.04 | 13.42 | 15% | 59% |
| 6 | fuori | 0.9 / 0.3 / 2.77 | 0.4 / 0.2 / 1.83 | 1.89 | 48% | 58% |
| 12 | tutto | 3.8 / 0.3 / 11.96 | 2.5 / 0.2 / 12.12 | 11.58 | 60% | 64% |
| 12 | dentro | 2.9 / 0.3 / 11.23 | 1.7 / 0.2 / 9.56 | 11.19 | 21% | 63% |
| 12 | fuori | 0.9 / 0.3 / 2.78 | 0.7 / 0.2 / 3.66 | 2.76 | 78% | 65% |

## Per famiglia (A, Spearman medio per strategia)

| famiglia | strategie | settimana L=4 | mese L=3 |
|---|---:|---:|---:|
| BBO | 5 | +0.038 | -0.006 |
| BIA | 2 | +0.012 | -0.061 |
| BOS | 16 | +0.012 | -0.054 |
| BRT | 2 | +0.002 | -0.129 |
| BSW | 5 | -0.003 | -0.107 |
| LFD | 25 | -0.016 | -0.051 |
| LFH | 26 | -0.023 | -0.031 |
| MAC | 23 | -0.012 | -0.087 |
| PCH | 23 | +0.017 | -0.096 |
| RBM | 16 | -0.018 | -0.001 |
| RBU | 12 | -0.015 | -0.125 |
| RHL | 15 | -0.012 | -0.072 |
| SBO | 15 | +0.019 | -0.043 |
| TFM | 12 | -0.102 | -0.083 |
| TFU | 3 | -0.002 | -0.077 |
| VBO | 18 | +0.010 | +0.001 |

