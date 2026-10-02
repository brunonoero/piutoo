// Manuale: numera i capitoli, costruisce l'indice a sinistra, segue lo scorrimento, filtra per parola.
(function () {
  "use strict";

  var doc = document.getElementById("doc");
  var toc = document.getElementById("toc");
  if (!doc || !toc) return;

  var sections = Array.prototype.slice.call(doc.querySelectorAll("section[id]"));
  var html = '<input type="search" placeholder="Cerca nel manuale…" aria-label="Cerca nel manuale"><ol>';
  var lastPart = null;
  var slugCount = 0;

  sections.forEach(function (section, index) {
    var heading = section.querySelector("h2");
    var number = index + 1;
    var title = heading.textContent;
    heading.innerHTML = '<span class="num">' + (number < 10 ? "0" : "") + number + "</span>" + heading.innerHTML;

    var part = section.getAttribute("data-part");
    if (part !== lastPart) {
      html += '<li class="part">' + part + "</li>";
      lastPart = part;
    }

    var subs = Array.prototype.slice.call(section.querySelectorAll("h3"));
    var subHtml = subs.map(function (sub) {
      if (!sub.id) sub.id = section.id + "-" + (++slugCount);
      return '<li><a href="#' + sub.id + '">' + sub.textContent + "</a></li>";
    }).join("");

    html += '<li data-section="' + section.id + '"><a href="#' + section.id + '">' + title + "</a>" +
      (subHtml ? "<ol>" + subHtml + "</ol>" : "") + "</li>";
  });
  toc.innerHTML = html + "</ol>";

  // ── Evidenzia il capitolo che si sta leggendo ───────────────────────────
  var links = {};
  Array.prototype.forEach.call(toc.querySelectorAll("li[data-section] > a"), function (link) {
    links[link.getAttribute("href").slice(1)] = link;
  });

  function markActive() {
    var current = sections[0];
    for (var i = 0; i < sections.length; i++) {
      if (sections[i].getBoundingClientRect().top <= 120) current = sections[i];
    }
    Object.keys(links).forEach(function (id) {
      links[id].classList.toggle("active", id === current.id);
    });
  }
  window.addEventListener("scroll", markActive, { passive: true });
  markActive();

  // ── Ricerca: restano nell'indice i capitoli che contengono la parola ────
  var search = toc.querySelector("input");
  search.addEventListener("input", function () {
    var query = search.value.trim().toLowerCase();
    var visibleParts = {};
    sections.forEach(function (section) {
      var hit = !query || section.textContent.toLowerCase().indexOf(query) >= 0;
      var item = toc.querySelector('li[data-section="' + section.id + '"]');
      item.classList.toggle("nohit", !hit);
      if (hit) visibleParts[section.getAttribute("data-part")] = true;
    });
    Array.prototype.forEach.call(toc.querySelectorAll("li.part"), function (part) {
      part.classList.toggle("nohit", !visibleParts[part.textContent]);
    });
  });
})();
