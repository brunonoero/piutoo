// Pagina "Piani per broker": disegna window.PIOOTOO_PIANI (data/piani.js, generato da aggiorna-dati.py).
(function () {
  "use strict";

  var data = window.PIOOTOO_PIANI;
  var P = window.Piootoo;
  var esc = P.escapeHtml;
  var app = document.getElementById("app");

  if (!data) {
    app.innerHTML = '<div class="warnings">Mancano i dati: lancia <code>python aggiorna-dati.py</code> dalla cartella <code>web</code>.</div>';
    return;
  }

  var VERDICTS = {
    ok:     { icon: "✓", label: "Compatibile", line: "Può stare su un altro conto con" },
    limite: { icon: "~", label: "Al limite", line: "Al limite con" },
    no:     { icon: "✕", label: "Non compatibile", line: "Non può stare con" },
    stesso: { icon: "=", label: "Stesso piano", line: "Stesse strategie di" },
    nd:     { icon: "?", label: "Non misurato", line: "Non misurato con" }
  };
  var VERDICT_ORDER = ["ok", "limite", "no", "stesso", "nd"];
  var GROUPS = [
    { state: "produzione", title: "In produzione — broker workspace" },
    { state: "best-plan", title: "Best plan — non ancora in produzione" },
    { state: "ritirato", title: "Ritirati" }
  ];
  // Una tinta per simbolo, cosi' le strategie dello stesso mercato si riconoscono a colpo d'occhio.
  var SYMBOL_HUES = { FDAX: 215, FESX: 250, NQ: 285, ES: 330, YM: 12, GC: 42, KC: 25, BP: 165, CL: 190, BTC: 95, CC: 60, NG: 130 };

  var state = { broker: null, query: "", pair: null };

  function brokerByCode(code) {
    for (var i = 0; i < data.brokers.length; i++) if (data.brokers[i].code === code) return data.brokers[i];
    return null;
  }
  function pairOf(broker, a, b) {
    return broker.pairs[a + "|" + b] || broker.pairs[b + "|" + a] || null;
  }
  function activePlans(broker) {
    return broker.plans.filter(function (plan) { return plan.state !== "ritirato"; });
  }
  function countBy(broker, planState) {
    return broker.plans.filter(function (plan) { return plan.state === planState; }).length;
  }

  // ── Pastiglie ────────────────────────────────────────────────────────────
  function strategyChip(code, weight) {
    var parts = code.split("_");
    var series = parts[0] || "";
    var symbol = parts[1] || "";
    var rest = parts.slice(2).join("_");
    var hue = SYMBOL_HUES[symbol] != null ? SYMBOL_HUES[symbol] : 200;
    var weightHtml = weight != null ? '<span class="w" title="Peso della strategia nel piano">×' + P.formatNumber(weight, 2) + "</span>" : "";
    return '<span class="strat" data-code="' + esc(code) + '" title="' + esc(code) + '">' +
      '<span class="sym" style="--h:' + hue + '">' + esc(symbol) + "</span>" +
      "<span>" + esc(series) + " " + esc(rest) + "</span>" + weightHtml + "</span>";
  }

  function sortStrategies(codes) {
    return codes.slice().sort(function (a, b) {
      var sa = a.split("_")[1] || "", sb = b.split("_")[1] || "";
      return sa === sb ? a.localeCompare(b) : sa.localeCompare(sb);
    });
  }

  function pairTitle(pair) {
    var text = pair.reason;
    if (pair.shared) text += " " + pair.shared.join(", ");
    if (pair.note) text += " " + pair.note;
    return text;
  }

  // ── Righe della tabella ──────────────────────────────────────────────────
  function holdingText(plan) {
    if (plan.overnight && plan.overweek) return "overnight e overweek";
    if (plan.overnight) return "overnight, senza overweek";
    return "intraday";
  }

  function accountText(broker, number) {
    for (var i = 0; i < broker.accounts.length; i++) {
      if (broker.accounts[i].number === number) return number + " · " + broker.accounts[i].name;
    }
    return number;
  }

  function planCell(plan) {
    var origin = "";
    if (plan.state === "best-plan") {
      origin = "Workspace <code>" + esc(plan.origin.workspace) + "</code> · best plan dal " + P.formatDate(plan.promotedUtc);
      if (plan.olderCards) origin += " · " + plan.olderCards + (plan.olderCards === 1 ? " scheda precedente" : " schede precedenti");
    } else {
      origin = "Dal best plan <code>" + esc(plan.origin.planCode) + "</code> · in produzione dal " + P.formatDate(plan.promotedUtc);
      if (plan.origin.previousPlanCode) origin += " · sostituisce <code>" + esc(plan.origin.previousPlanCode) + "</code>";
      if (plan.retiredUtc) origin += " · ritirato il " + P.formatDate(plan.retiredUtc);
    }
    return '<div class="plan-code">' + esc(plan.code) + "</div>" +
      '<div class="plan-name">' + esc(plan.name) + "</div>" +
      (plan.note ? '<div class="plan-note">' + esc(plan.note) + "</div>" : "") +
      '<div class="plan-origin">' + origin + "</div>";
  }

  function setupCell(broker, plan) {
    var accounts = plan.accounts.map(function (n) { return esc(accountText(broker, n)); }).join("<br>");
    var rows = [
      ["Conto", accounts || "—", false],
      ["Size", "× " + P.formatNumber(plan.size, 2), true],
      ["Tenuta", holdingText(plan), false],
      ["Flat", plan.sessionFlatUtc ? plan.sessionFlatUtc + " UTC" : "—", false],
      ["Commiss.", plan.commission != null ? P.formatNumber(plan.commission, 2) : "—", false]
    ];
    return '<dl class="kv">' + rows.map(function (row) {
      return "<dt>" + row[0] + "</dt><dd" + (row[2] ? ' class="strong"' : "") + ">" + row[1] + "</dd>";
    }).join("") + "</dl>";
  }

  function runCell(plan) {
    var run = plan.run;
    if (!run) return '<span class="stamp">—</span>';
    var net = (run.netProfit >= 0 ? "+" : "") + P.formatNumber(run.netProfit);
    var rows = [
      ["Periodo", '<span title="' + P.formatDate(run.startUtc) + " → " + P.formatDate(run.endUtc) + '">' +
        P.formatDate(run.startUtc).slice(3) + " → " + P.formatDate(run.endUtc).slice(3) + "</span>"],
      ["Trade", P.formatNumber(run.trades)],
      ["Netto", net],
      ["DD max", P.formatNumber(run.maxDrawdown)]
    ];
    return '<dl class="kv">' + rows.map(function (row) {
      return "<dt>" + row[0] + "</dt><dd>" + row[1] + "</dd>";
    }).join("") + "</dl>";
  }

  function strategiesCell(plan) {
    var chips = sortStrategies(plan.strategies).map(function (code) {
      return strategyChip(code, plan.weights ? plan.weights[code] : null);
    }).join("");
    return '<div class="count">' + plan.strategies.length + " strategie</div><div class=\"chips\">" + chips + "</div>";
  }

  function compatCell(broker, plan) {
    if (plan.state === "ritirato") return '<span class="stamp">Piano ritirato: le sue strategie sono libere.</span>';
    var others = activePlans(broker).filter(function (other) { return other.code !== plan.code; });
    if (!others.length) return '<span class="stamp">Unico piano promosso su questo broker.</span>';

    var byVerdict = {};
    others.forEach(function (other) {
      var pair = pairOf(broker, plan.code, other.code);
      if (!pair) return;
      (byVerdict[pair.verdict] = byVerdict[pair.verdict] || []).push({ other: other, pair: pair });
    });

    return VERDICT_ORDER.filter(function (verdict) { return byVerdict[verdict]; }).map(function (verdict) {
      var chips = byVerdict[verdict].map(function (item) {
        var detail = item.pair.twins ? " <small>" + esc(item.pair.twins) + "</small>"
          : item.pair.shared && verdict === "no" ? " <small>" + item.pair.shared.length + " in comune</small>" : "";
        return '<button type="button" class="chip ' + verdict + '" data-pair="' + esc(plan.code + "|" + item.other.code) +
          '" title="' + esc(pairTitle(item.pair)) + '">' + esc(item.other.code) + detail + "</button>";
      }).join("");
      return '<div class="compat-line" title="' + VERDICTS[verdict].line + '">' +
        '<span class="compat-ico ' + verdict + '">' + VERDICTS[verdict].icon + "</span>" +
        '<div class="chips">' + chips + "</div></div>";
    }).join("");
  }

  function plansTable(broker) {
    if (!broker.plans.length) {
      return '<div class="card empty">Nessun piano promosso su questo broker: né in produzione, né fra i best plan.</div>';
    }
    var body = GROUPS.map(function (group) {
      var plans = broker.plans.filter(function (plan) { return plan.state === group.state; });
      if (!plans.length) return "";
      var rows = plans.map(function (plan) {
        var haystack = (plan.code + " " + plan.name + " " + plan.strategies.join(" ")).toLowerCase();
        return '<tr class="plan" data-search="' + esc(haystack) + '">' +
          '<td class="col-plan">' + planCell(plan) + "</td>" +
          '<td><span class="badge ' + plan.state + '">' + (plan.state === "produzione" ? "In produzione" : plan.state === "best-plan" ? "Best plan" : "Ritirato") + "</span></td>" +
          "<td>" + setupCell(broker, plan) + "</td>" +
          "<td>" + runCell(plan) + "</td>" +
          '<td class="col-strats">' + strategiesCell(plan) + "</td>" +
          '<td class="col-compat">' + compatCell(broker, plan) + "</td></tr>";
      }).join("");
      return '<tr class="group"><td colspan="6">' + group.title + " · " + plans.length + "</td></tr>" + rows;
    }).join("");

    return '<div class="card plans-card"><table class="plans">' +
      '<colgroup><col class="c-plan"><col class="c-state"><col class="c-setup"><col class="c-run"><col class="c-strats"><col class="c-compat"></colgroup>' +
      "<thead><tr>" +
      "<th>Piano</th><th>Stato</th><th>Conto e size</th><th>Run del best plan</th><th>Strategie</th>" +
      "<th>Su un altro conto con</th>" +
      "</tr></thead><tbody>" + body + "</tbody></table></div>";
  }

  // ── Matrice ──────────────────────────────────────────────────────────────
  function matrix(broker) {
    var plans = activePlans(broker);
    if (plans.length < 2) return "";

    var head = "<tr><th></th>" + plans.map(function (plan) {
      return '<th class="' + (plan.state === "produzione" ? "prod" : "") + '"><div>' + esc(plan.code) + "</div></th>";
    }).join("") + "</tr>";

    var rows = plans.map(function (rowPlan, r) {
      var cells = plans.map(function (colPlan, c) {
        if (r === c) return '<td class="diag"></td>';
        var pair = pairOf(broker, rowPlan.code, colPlan.code);
        var verdict = pair ? pair.verdict : "nd";
        return '<td><button type="button" class="cell ' + verdict + '" data-pair="' + esc(rowPlan.code + "|" + colPlan.code) +
          '" title="' + esc(rowPlan.code + " × " + colPlan.code + ": " + (pair ? pairTitle(pair) : "")) + '">' +
          VERDICTS[verdict].icon + "</button></td>";
      }).join("");
      return '<tr><th class="' + (rowPlan.state === "produzione" ? "prod" : "") + '">' + esc(rowPlan.code) + "</th>" + cells + "</tr>";
    }).join("");

    var legend = VERDICT_ORDER.map(function (verdict) {
      return '<span><i class="cell ' + verdict + '">' + VERDICTS[verdict].icon + "</i>" + VERDICTS[verdict].label + "</span>";
    }).join("");

    return '<h2 class="block-title">Chi può stare con chi</h2>' +
      '<p class="block-sub">' + esc(data.criterion) + " In verde i codici dei piani in produzione.</p>" +
      '<div class="card matrix-wrap"><div><div class="table-wrap"><table class="matrix"><thead>' + head + "</thead><tbody>" + rows +
      '</tbody></table></div><div class="legend">' + legend + "</div></div>" +
      '<div class="pair-detail" id="pair-detail"><p class="hint">Seleziona una casella, o un piano nella colonna «Su un altro conto con», per leggere il motivo.</p></div></div>';
  }

  function showPair(broker, key) {
    var codes = key.split("|");
    var pair = pairOf(broker, codes[0], codes[1]);
    var panel = document.getElementById("pair-detail");
    if (!pair || !panel) return;
    state.pair = key;

    var html = "<h4>" + esc(codes[0]) + " × " + esc(codes[1]) + "</h4>" +
      '<div class="verdict ' + pair.verdict + '">' + VERDICTS[pair.verdict].icon + " " + VERDICTS[pair.verdict].label + "</div>" +
      "<p>" + esc(pair.reason) + "</p>";
    if (pair.shared) {
      html += '<div class="chips">' + sortStrategies(pair.shared).map(function (code) { return strategyChip(code, null); }).join("") + "</div>";
    }
    if (pair.note) html += "<p>" + esc(pair.note) + "</p>";
    if (pair.measured) {
      html += '<p class="hint">Misurato il ' + P.formatDate(pair.measured) +
        (pair.measuredOn ? " sui piani con le stesse strategie: " + esc(pair.measuredOn) : "") + ".</p>";
    }
    panel.innerHTML = html;

    var buttons = app.querySelectorAll("table.matrix button");
    for (var i = 0; i < buttons.length; i++) {
      var k = buttons[i].getAttribute("data-pair");
      var same = k === key || k === codes[1] + "|" + codes[0];
      buttons[i].setAttribute("aria-pressed", same ? "true" : "false");
    }
  }

  // ── Combinazioni e note ──────────────────────────────────────────────────
  function combinations(broker) {
    if (!broker.combinations.length) return "";
    var shown = {};
    broker.plans.forEach(function (plan) {
      shown[plan.code] = plan.code;
      if (plan.state === "produzione" && plan.origin.planCode) shown[plan.origin.planCode] = plan.code;
    });
    var cards = broker.combinations.map(function (combo) {
      var chips = combo.piani.map(function (code) {
        return shown[code]
          ? '<span class="chip plain" title="' + esc(code) + '">' + esc(shown[code]) + "</span>"
          : '<span class="chip ghost" title="Non è fra i piani promossi su disco">' + esc(code) + "</span>";
      }).join('<span class="plus">+</span>');
      var title = combo.piani.length === 1 ? "Un conto in più" : combo.conti + " conti";
      return '<div class="combo"><div class="n">' + title + '</div><div class="chips">' + chips + "</div><p>" + esc(combo.nota || "") + "</p></div>";
    }).join("");
    return '<h2 class="block-title">Combinazioni su più conti</h2>' +
      '<p class="block-sub">Un piano per conto, tutti sullo stesso broker. Tratteggiati i piani citati dal registro che non sono fra i promossi.</p>' +
      '<div class="combos">' + cards + "</div>";
  }

  function notes(broker) {
    if (!broker.notes.length) return "";
    return '<h2 class="block-title">Note</h2><div class="card"><ul class="notes">' +
      broker.notes.map(function (note) { return "<li>" + esc(note) + "</li>"; }).join("") + "</ul></div>";
  }

  function facts(broker) {
    var accounts = broker.accounts.map(function (account) {
      return "<b>" + esc(account.number) + "</b> " + esc(account.name) + " · " + P.formatNumber(account.balance) + " " + esc(account.currency || "") +
        (account.enabled ? "" : " · disattivato");
    }).join(" &nbsp;|&nbsp; ");
    return '<div class="broker-facts">' +
      '<div class="fact"><span class="label">Conti in anagrafica</span><span>' + (accounts || "nessuno") + "</span></div>" +
      (broker.rules ? '<div class="fact"><span class="label">Regole</span><span>' + esc(broker.rules) + "</span></div>" : "") +
      "</div>";
  }

  // ── Ricerca ──────────────────────────────────────────────────────────────
  function applySearch() {
    var query = state.query.trim().toLowerCase();
    var rows = app.querySelectorAll("tr.plan");
    for (var i = 0; i < rows.length; i++) {
      var hit = !query || rows[i].getAttribute("data-search").indexOf(query) >= 0;
      rows[i].classList.toggle("hidden", !hit);
      var chips = rows[i].querySelectorAll(".strat");
      for (var j = 0; j < chips.length; j++) {
        var match = query && chips[j].getAttribute("data-code").toLowerCase().indexOf(query) >= 0;
        chips[j].classList.toggle("hit", !!match);
      }
    }
  }

  // ── Pagina ───────────────────────────────────────────────────────────────
  function render() {
    var broker = brokerByCode(state.broker) || data.brokers[0];
    state.broker = broker.code;

    var tabs = data.brokers.map(function (b) {
      var prod = countBy(b, "produzione"), best = countBy(b, "best-plan");
      var sub = b.plans.length ? prod + " in produzione · " + best + " best plan" : "nessun piano promosso";
      return '<button type="button" class="tab" role="tab" data-broker="' + esc(b.code) + '" aria-selected="' + (b.code === broker.code) + '">' +
        "<b>" + esc(b.name) + "</b><span>" + sub + "</span></button>";
    }).join("");

    var warnings = data.warnings && data.warnings.length
      ? '<div class="warnings"><b>Avvisi del generatore</b><ul>' + data.warnings.map(function (w) { return "<li>" + esc(w) + "</li>"; }).join("") + "</ul></div>"
      : "";

    app.innerHTML =
      '<div class="page-head"><h1>Piani promossi, per broker</h1>' +
      "<p>Per ogni broker: i piani in produzione e i best plan, con codice, nome, strategie e con chi possono stare su un altro conto dello stesso broker.</p>" +
      '<p class="stamp">Dati letti dal disco il ' + P.formatDateTime(data.generatedUtc) + " · per aggiornare: <code>python aggiorna-dati.py</code></p></div>" +
      warnings +
      '<div class="explain">' +
      '<div><span class="badge produzione">In produzione</span><p>Sta nel broker workspace: è il piano che va sui conti. ' +
      "Nasce solo da un best plan, con le strategie che il run ha eseguito; non si modifica, si duplica con altri conti o si ritira. " +
      'Dentro un broker una strategia sta in un solo piano attivo. <a href="manuale.html#come-produzione">Come funziona →</a></p></div>' +
      '<div><span class="badge best-plan">Best plan</span><p>Un backtest messo in evidenza: fotografia del run, con il piano bloccato da quel momento. ' +
      "Non è ancora su nessun conto: è il candidato alla produzione. " +
      '<a href="manuale.html#come-best-plan">Come funziona →</a></p></div>' +
      "</div>" +
      '<div class="toolbar"><div class="tabs" role="tablist">' + tabs + "</div>" +
      '<input class="search" type="search" placeholder="Cerca un piano o una strategia (es. NQ_BSW, GC, EUROPA)" value="' + esc(state.query) + '"></div>' +
      facts(broker) + plansTable(broker) + matrix(broker) + combinations(broker) + notes(broker);

    applySearch();
    if (state.pair) showPair(broker, state.pair);
  }

  app.addEventListener("click", function (event) {
    var tab = event.target.closest("[data-broker]");
    if (tab) {
      state.broker = tab.getAttribute("data-broker");
      state.pair = null;
      try { history.replaceState(null, "", "#" + state.broker); } catch (e) { /* file:// senza history */ }
      render();
      return;
    }
    var pairButton = event.target.closest("[data-pair]");
    if (pairButton) {
      showPair(brokerByCode(state.broker), pairButton.getAttribute("data-pair"));
      if (!pairButton.classList.contains("cell")) {
        var panel = document.getElementById("pair-detail");
        if (panel) panel.scrollIntoView({ block: "center", behavior: "smooth" });
      }
    }
  });
  app.addEventListener("input", function (event) {
    if (event.target.classList.contains("search")) {
      state.query = event.target.value;
      applySearch();
    }
  });

  state.broker = decodeURIComponent((location.hash || "").replace("#", "")) || null;
  render();
})();
