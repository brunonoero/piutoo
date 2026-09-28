# NQ BSW: stop largo, target ampio e calendario

Base: `PT5DAV_NQ_BSW_001_240` (long lunedi' 04:00 → giovedi' 04:00, ora della ricerca, nessuno stop ne' target). Stop e target in ATR50 delle sessioni dal prezzo d'ingresso. Costi FTMO: spread 1.45 punti, swap long 6.7098 punti a notte. Orologio al minuto, un contratto.

**Swap sui periodi interni** riportato al prezzo d'ingresso di ogni trade (× prezzo / 30,400, il Nasdaq del giorno della scheda FTMO): applicato in punti fissi al Nasdaq del 2012 sarebbe un finanziamento di quasi il 100% l'anno. Sul feed FTMO resta quello misurato. Lo spread resta in punti fissi anche sui periodi interni (pesa meno di 30 dollari a trade).

**Regola di scelta, scritta prima dei risultati**, sul solo periodo 2012-2020: fra le configurazioni con stop acceso, quelle con netto almeno l'85% della base e net/DD almeno quello della base; fra queste la migliore per net/DD; plateau: la media net/DD dei vicini (stop e target adiacenti) almeno l'80% della scelta. Nessuna che passi = la base resta com'e'. Gli altri due periodi non entrano nella scelta.

## La scelta (interno 2012-2020)

Base: 462 trade, netto 110,572, DD 25,381, net/DD 4.36, peggior trade -11,977, swap 47,240. Configurazioni ammesse dalla regola: 6 su 36.

- stop 4 target 3: net/DD 5.01, plateau 4.47 su 3 vicini → **scelta**


**Scelta: stop 4 ATR, target 3 ATR** (target acceso).

## Base e scelta sui tre periodi

| periodo | ruolo | configurazione | trade | netto | DD chiuso | net/DD | peggior trade | swap | usciti a stop | usciti a target |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| interno 2012-2020 | scelta di stop e target | base | 462 | 110,572 | 25,381 | 4.36 | -11,977 | 47,240 | 0 | 0 |
| interno 2012-2020 | scelta di stop e target | stop 4 / target 3 | 462 | 116,276 | 23,204 | 5.01 | -11,977 | 46,798 | 3 | 5 |
| interno 2021-05/2025 | fuori campione per la scelta, in campione per la base | base | 226 | 161,371 | 36,690 | 4.40 | -14,884 | 51,620 | 0 | 0 |
| interno 2021-05/2025 | fuori campione per la scelta, in campione per la base | stop 4 / target 3 | 226 | 144,984 | 36,690 | 3.95 | -14,884 | 51,395 | 0 | 1 |
| FTMO 06/2025-09/2026 | fuori campione per tutto | base | 68 | 162,558 | 34,873 | 4.66 | -19,456 | 27,376 | 0 | 0 |
| FTMO 06/2025-09/2026 | fuori campione per tutto | stop 4 / target 3 | 68 | 162,558 | 34,873 | 4.66 | -19,456 | 27,376 | 0 | 0 |

## Griglia completa: interno 2012-2020 (scelta di stop e target)

net/DD, e fra parentesi il netto in migliaia. Righe = stop in ATR (0 = nessuno), colonne = target in ATR (0 = nessuno).

| stop \ target | 0 | 3 | 4 | 5 | 6 | 8 |
|---|---:|---:|---:|---:|---:|---:|
| 0 | 4.36 (111) | 4.81 (115) | 4.36 (111) | 4.36 (111) | 4.36 (111) | 4.36 (111) |
| 1 | 4.33 (84) | 4.45 (86) | 4.33 (84) | 4.33 (84) | 4.33 (84) | 4.33 (84) |
| 1.5 | 2.64 (77) | 2.80 (82) | 2.64 (77) | 2.64 (77) | 2.64 (77) | 2.64 (77) |
| 2 | 2.40 (78) | 2.55 (83) | 2.40 (78) | 2.40 (78) | 2.40 (78) | 2.40 (78) |
| 2.5 | 2.89 (85) | 3.05 (90) | 2.89 (85) | 2.89 (85) | 2.89 (85) | 2.89 (85) |
| 3 | 3.89 (93) | 4.27 (98) | 3.89 (93) | 3.89 (93) | 3.89 (93) | 3.89 (93) |
| 4 | 4.58 (111) | 5.01 (116) | 4.58 (111) | 4.58 (111) | 4.58 (111) | 4.58 (111) |

## Griglia completa: interno 2021-05/2025 (fuori campione per la scelta, in campione per la base)

net/DD, e fra parentesi il netto in migliaia. Righe = stop in ATR (0 = nessuno), colonne = target in ATR (0 = nessuno).

| stop \ target | 0 | 3 | 4 | 5 | 6 | 8 |
|---|---:|---:|---:|---:|---:|---:|
| 0 | 4.40 (161) | 3.95 (145) | 4.19 (154) | 4.43 (163) | 4.40 (161) | 4.40 (161) |
| 1 | 0.94 (56) | 0.94 (56) | 0.94 (56) | 0.94 (56) | 0.94 (56) | 0.94 (56) |
| 1.5 | 2.09 (87) | 1.70 (71) | 1.91 (80) | 2.12 (89) | 2.09 (87) | 2.09 (87) |
| 2 | 1.23 (85) | 0.99 (68) | 1.12 (77) | 1.25 (86) | 1.23 (85) | 1.23 (85) |
| 2.5 | 2.38 (118) | 2.04 (101) | 2.22 (110) | 2.40 (119) | 2.38 (118) | 2.38 (118) |
| 3 | 4.11 (151) | 3.66 (134) | 3.90 (143) | 4.14 (152) | 4.11 (151) | 4.11 (151) |
| 4 | 4.40 (161) | 3.95 (145) | 4.19 (154) | 4.43 (163) | 4.40 (161) | 4.40 (161) |

## Griglia completa: FTMO 06/2025-09/2026 (fuori campione per tutto)

net/DD, e fra parentesi il netto in migliaia. Righe = stop in ATR (0 = nessuno), colonne = target in ATR (0 = nessuno).

| stop \ target | 0 | 3 | 4 | 5 | 6 | 8 |
|---|---:|---:|---:|---:|---:|---:|
| 0 | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) |
| 1 | 2.31 (95) | 2.31 (95) | 2.31 (95) | 2.31 (95) | 2.31 (95) | 2.31 (95) |
| 1.5 | 3.30 (138) | 3.30 (138) | 3.30 (138) | 3.30 (138) | 3.30 (138) | 3.30 (138) |
| 2 | 2.48 (123) | 2.48 (123) | 2.48 (123) | 2.48 (123) | 2.48 (123) | 2.48 (123) |
| 2.5 | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) |
| 3 | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) |
| 4 | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) | 4.66 (163) |

## Il calendario: la stessa tenuta negli altri giorni, senza stop ne' target

| periodo | finestra | trade | netto | DD chiuso | net/DD | peggior trade | swap |
|---|---|---:|---:|---:|---:|---:|---:|
| interno 2012-2020 | lun-gio (la strategia) | 462 | 110,572 | 25,381 | 4.36 | -11,977 | 47,240 |
| interno 2012-2020 | mar-ven | 464 | 56,920 | 44,742 | 1.27 | -23,245 | 47,449 |
| interno 2012-2020 | mer-lun | 462 | -16,876 | 57,043 | -0.30 | -18,924 | 78,763 |
| interno 2012-2020 | gio-mar | 464 | 27,095 | 64,425 | 0.42 | -17,645 | 79,219 |
| interno 2012-2020 | ven-mer | 457 | 24,926 | 55,396 | 0.45 | -13,412 | 77,786 |
| interno 2012-2020 | settimana intera lun-lun | 231 | 71,829 | 52,378 | 1.37 | -15,116 | 54,892 |
| interno 2021-05/2025 | lun-gio (la strategia) | 226 | 161,371 | 36,690 | 4.40 | -14,884 | 51,620 |
| interno 2021-05/2025 | mar-ven | 229 | -13,524 | 80,271 | -0.17 | -23,584 | 52,233 |
| interno 2021-05/2025 | mer-lun | 227 | -103,964 | 205,957 | -0.50 | -52,647 | 86,131 |
| interno 2021-05/2025 | gio-mar | 229 | -62,672 | 152,690 | -0.41 | -26,120 | 87,161 |
| interno 2021-05/2025 | ven-mer | 225 | 22,367 | 118,463 | 0.19 | -30,875 | 85,508 |
| interno 2021-05/2025 | settimana intera lun-lun | 113 | -29,313 | 128,851 | -0.23 | -45,769 | 60,071 |
| FTMO 06/2025-09/2026 | lun-gio (la strategia) | 68 | 162,558 | 34,873 | 4.66 | -19,456 | 27,376 |
| FTMO 06/2025-09/2026 | mar-ven | 68 | 24,216 | 70,241 | 0.34 | -28,014 | 27,376 |
| FTMO 06/2025-09/2026 | mer-lun | 67 | -32,831 | 127,291 | -0.26 | -31,011 | 44,956 |
| FTMO 06/2025-09/2026 | gio-mar | 65 | 26,173 | 81,994 | 0.32 | -23,652 | 43,614 |
| FTMO 06/2025-09/2026 | ven-mer | 67 | 82,277 | 32,611 | 2.52 | -23,500 | 44,956 |
| FTMO 06/2025-09/2026 | settimana intera lun-lun | 34 | -7,228 | 55,380 | -0.13 | -28,369 | 31,939 |

## Verdetto (29/09/2026)

**Cosa regge.**
- *Il calendario non e' la sola deriva del Nasdaq.* Lunedi'-giovedi' batte le altre quattro finestre di tre
  sessioni e la settimana intera in tutti e tre i periodi, e soprattutto sul feed FTMO 06/2025-09/2026, fuori
  campione per tutto: net/DD 4,66 contro 2,52 della seconda (ven-mer) e -0,13 della settimana intera. Nei
  periodi interni il confronto e' in campione per la base (la ricerca PT5DAV ha scelto i giorni sul
  2012-2025), sul feed FTMO no.

**Cosa non regge.**
- *Uno stop che scatta davvero costa caro.* Da 1 a 2,5 ATR lo stop taglia i ribassi che dentro la settimana
  rientrano: sul 2012-2020 il net/DD scende da 4,36 a 2,4-2,9, sul feed FTMO da 4,66 a 2,3-3,3.
- *La configurazione scelta dalla regola (stop 4, target 3) non cambia niente di utile.* Lo stop a 4 ATR
  scatta 3 volte su 462 nel periodo di scelta e mai dopo; il miglioramento viene dal target a 3 ATR (5 uscite),
  che nel 2021-05/2025 peggiora (net/DD 3,95 contro 4,40) e sul feed FTMO non scatta mai. Il peggior trade
  resta lo stesso in ogni periodo.

**Decisione.** `PT5DAV_NQ_BSW_001_240` resta com'e', senza stop e senza target. Il rischio della coda (29/07/2026:
-939 punti, circa 2 ATR) si governa con la size e con il peso nel piano, non con uno stop: uno stop abbastanza
stretto da prendere quel giorno toglie meta' del rendimento.

**Nota di metodo.** Il primo giro dello studio applicava lo swap FTMO in punti fissi (6,71 a notte) anche al
Nasdaq del 2012 e faceva perdere tutte le finestre long, settimana intera compresa. Lo stesso difetto tocca il
percorso sulla storia lunga del feed interno per le configurazioni che tengono la notte.
