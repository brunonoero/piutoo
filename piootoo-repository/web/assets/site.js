// Parti comuni a tutte le pagine: barra in alto e tema chiaro/scuro.
(function () {
  "use strict";

  var PAGES = [
    { href: "index.html", label: "Home" },
    { href: "manuale.html", label: "Manuale d'uso" },
    { href: "piani.html", label: "Piani per broker" }
  ];

  // localStorage puo' mancare (file:// in alcune configurazioni): il sito deve reggere senza.
  function readTheme() {
    try { return localStorage.getItem("piootoo-tema"); } catch (e) { return null; }
  }
  function writeTheme(value) {
    try {
      if (value) localStorage.setItem("piootoo-tema", value);
      else localStorage.removeItem("piootoo-tema");
    } catch (e) { /* si resta sul tema di sistema */ }
  }
  function systemIsDark() {
    return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
  }

  var stored = readTheme();
  if (stored) document.documentElement.setAttribute("data-theme", stored);

  function buildTopbar() {
    var here = (location.pathname.split("/").pop() || "index.html").toLowerCase();
    var bar = document.createElement("header");
    bar.className = "topbar";

    var links = PAGES.map(function (page) {
      var current = page.href === here ? ' aria-current="page"' : "";
      return '<a href="' + page.href + '"' + current + ">" + page.label + "</a>";
    }).join("");

    bar.innerHTML =
      '<a class="brand" href="index.html"><img class="mark" src="assets/logo.svg" alt="" width="30" height="30">' +
      '<span class="word">Pi<span class="oo">oo</span>t<span class="oo">oo</span></span> <small>manuale e piani</small></a>' +
      '<nav class="topnav">' + links + "</nav>" +
      '<span class="spacer"></span>' +
      '<button class="theme-toggle" type="button" title="Cambia tema chiaro / scuro" aria-label="Cambia tema chiaro / scuro">' +
      '<svg viewBox="0 0 20 20" width="17" height="17" aria-hidden="true"><circle cx="10" cy="10" r="7.25" fill="none" stroke="currentColor" stroke-width="1.5"/>' +
      '<path d="M10 2.75a7.25 7.25 0 0 1 0 14.5z" fill="currentColor"/></svg></button>';
    document.body.insertBefore(bar, document.body.firstChild);

    bar.querySelector(".theme-toggle").addEventListener("click", function () {
      var current = document.documentElement.getAttribute("data-theme") || (systemIsDark() ? "dark" : "light");
      var next = current === "dark" ? "light" : "dark";
      document.documentElement.setAttribute("data-theme", next);
      writeTheme(next);
    });
  }

  if (document.body) buildTopbar();
  else document.addEventListener("DOMContentLoaded", buildTopbar);

  // Utilita' condivise dalle pagine.
  window.Piootoo = {
    escapeHtml: function (text) {
      return String(text == null ? "" : text)
        .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    },
    // "2026-09-28T20:10:53Z" -> "28/09/2026"
    formatDate: function (iso) {
      if (!iso) return "";
      var m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
      return m ? m[3] + "/" + m[2] + "/" + m[1] : iso;
    },
    formatDateTime: function (iso) {
      if (!iso) return "";
      var m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(iso);
      return m ? m[3] + "/" + m[2] + "/" + m[1] + " " + m[4] + ":" + m[5] + " UTC" : iso;
    },
    formatNumber: function (value, decimals) {
      if (value == null || isNaN(value)) return "";
      return Number(value).toLocaleString("it-IT", {
        minimumFractionDigits: decimals || 0, maximumFractionDigits: decimals == null ? 0 : decimals
      });
    }
  };
})();
