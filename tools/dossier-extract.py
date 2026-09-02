"""Estrae dal dossier del paniere le schede indicate, in un unico file di lavoro."""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOSSIER = ROOT / "piootoo-repository/run-engine/run-08-settembre/DOSSIER_PANIERE (1).md"


def main(out_path, wanted):
    raw = DOSSIER.read_text(encoding="utf-8")
    blocks = re.split(r"^### ", raw, flags=re.M)[1:]
    picked = [b for b in blocks
              if (m := re.match(r"(S\d+) ·", b)) and m.group(1) in wanted]
    Path(out_path).write_text(
        "".join("### " + b.rstrip() + "\n\n" for b in picked), encoding="utf-8")
    print(f"{out_path}: {len(picked)} schede")


if __name__ == "__main__":
    main(sys.argv[1], set(sys.argv[2:]))
