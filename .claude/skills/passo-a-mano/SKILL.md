---
name: passo-a-mano
description: >
  Il passo manuale su una strategia Piootoo: cambiare un parametro con un'ipotesi a priori e
  misurarlo dentro e fuori campione, come per PT3B_FDAX_PCH_002_240. Usala quando si chiede di
  "provare a mano", "cambiare un parametro", "aggiungere l'uscita alle 21" o applicare a una
  strategia esistente una leva già validata altrove.
---

# Il passo a mano

**Bozza del 23/09/2026.** Deriva da un caso solo che ha funzionato, la 002 del DAX, e dalle sue
obiezioni (F1 in `docs/lavori-in-corso.md`). Va rivisto quando ce ne saranno altri.

## Perché ha funzionato sul DAX

Tre cose insieme, e servono tutte: **una base con un edge** (la 001 stava in piedi, 787 trade e
utile da entrambe le parti); **un'ipotesi a priori** che veniva dai costi e non dai numeri (la
posizione attraversava il rollover e pagava il finanziamento: chiudi prima); **un grado di libertà**
(un parametro, `ExitHour = 21`). Il manuale **non crea un edge, lo pulisce**.

## Le regole

1. **Serve una base.** Su una cella dove il motore nudo non ha niente sopra soglia (GC, NQ 4h, CL,
   BP con il Price Channel) non c'è un parametro da girare: qualunque passo a mano lì tara il rumore.
2. **L'ipotesi si scrive prima** della misura, nel rapporto o nel commento della classe, e viene dai
   costi (rollover, spread per ora, commissione), dalla struttura della sessione (ore in cui il
   future è chiuso e quota il CFD) o da un filtro **già validato su un'altra cella e su un periodo
   lungo**. Mai da "guardo dove perde".
3. **Un parametro alla volta.** Due insieme sono una griglia da quattro celle senza dirlo.
4. **I tentativi si contano** e si scrivono tutti, anche quelli falliti: dieci prove a mano sono una
   griglia da dieci con lo stesso rischio di fortuna. Se servono più di tre, non è un passo a mano:
   è una griglia, e si lancia come tale (`/griglia-grossa`).
5. **Il verdetto dentro il campione prima che fuori** (`/lettura-risultati`), poi la verifica sul
   periodo lungo con lo stesso split delle altre celle. Il fuori campione non si guarda per
   scegliere il valore: si guarda una volta, dopo.
6. **La misura si fa con lo strumento del progetto**, non a occhio: `piootoo-sweep --params
   "Chiave=valore;..."` con i costi veri e lo split dichiarato, oppure uno studio come
   `Pt3b002LongPeriodTests`. Un minuto di macchina.
7. **Se regge, nasce una classe accanto**, non una modifica della base: la 001 resta confrontabile
   con i suoi run. Vedi `/promuovi-finalista`.

## Candidati sensati oggi

- Filtri già validati su una cella, portati su una regione nuda dello stesso mercato (i due filtri
  della 002 sulla regione canale 20 / solo long / uscita 21: è la misura A della coda del 23/09).
- L'uscita prima del rollover su strategie multiday che pagano swap (le TF a 15 minuti di NQ), ora
  che `SessionExitTime` vale per ogni motore.

## Dove non applicarla

- Su finaliste il cui fuori campione è un rally (GC 2025-26): il passo a mano lo asseconda soltanto.
- Con meno di ~200 trade in campione: il rischio di tarare rumore supera il guadagno atteso.
- Per "recuperare" una strategia bocciata: bocciata resta.
