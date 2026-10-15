"""Build a local evidence report, or check the recorded release decision.

This tool checks the completeness/consistency of the recorded evidence only.
It cannot determine copyright ownership or independently validate a licence.
It never edits game files, catalogues, preferences, or network services.
"""
import argparse
import hashlib
import html
import json
from collections import Counter
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / "Legal/character-rights-20261015.json"


def local_path(relative):
    result = (ROOT / relative).resolve()
    if not result.is_relative_to(ROOT):
        raise ValueError("Evidence path must stay inside the project: " + relative)
    return result


def load():
    data = json.loads(AUDIT.read_text(encoding="utf-8-sig"))
    names_source = (ROOT / "Assets/Scripts/PlanetGame.cs").read_text(encoding="utf-8-sig")
    match = re.search(r'characters\s*=\s*\{([^}]+)\}', names_source)
    if not match:
        raise ValueError("Cannot read current runtime roster")
    names = [s.strip() for s in re.findall(r'"([^"]+)"', match.group(1))]
    entries = data["characters"]
    if [c["slot"] for c in entries] != list(range(len(names))):
        raise ValueError("Audit slots do not match the runtime roster")
    source_ids = {source["id"] for source in data["sources"]}
    if len(source_ids) != len(data["sources"]):
        raise ValueError("Duplicate source id")
    for c, name in zip(entries, names):
        if c["name"].casefold() != name.casefold():
            raise ValueError("Changed hero name requires a new audit: " + name)
        if c["status"] not in {"claim_found", "unresolved", "cleared"}:
            raise ValueError("Unknown audit status")
        if not c["evidence"] or not set(c["evidence"]).issubset(source_ids):
            raise ValueError("Missing source reference: " + name)
        if not local_path(c["model"]).is_file() or not local_path(c["preview"]).is_file():
            raise ValueError("Missing active model or preview: " + name)
        if c["release_allowed"]:
            proof = c.get("grant_document")
            if c["status"] != "cleared" or not proof or not local_path(proof).is_file():
                raise ValueError("A release decision requires a local evidence document: " + name)
    return data


def inventory(data):
    selected = {"Legal/character-rights-20261015.json", "Legal/replacement-concept-prompt-20261015.txt",
                "Tools/character_rights_audit.py"}
    if (ROOT / "Legal/replacement-five-concepts-20261015.png").is_file():
        selected.add("Legal/replacement-five-concepts-20261015.png")
    for c in data["characters"]:
        selected.update([c["model"], c["preview"]])
    for finding in data["asset_findings"]:
        for relative in finding["paths"]:
            p = local_path(relative)
            if p.is_file():
                selected.add(relative)
            elif p.is_dir():
                selected.update(str(f.relative_to(ROOT)).replace("\\", "/")
                                for f in p.rglob("*") if f.is_file() and f.suffix != ".meta")
            else:
                raise ValueError("Missing asset evidence: " + relative)
    files = []
    for relative in sorted(selected):
        p = local_path(relative)
        with p.open("rb") as stream:
            sha256 = hashlib.file_digest(stream, "sha256").hexdigest()
        files.append(dict(path=relative, bytes=p.stat().st_size,
                          sha256=sha256))
    skins = json.loads((ROOT / "Content/Skins/catalog-cast-v1.json").read_text(encoding="utf-8-sig"))["skins"]
    products = json.loads((ROOT / "Content/Store/catalog-test-v1.json").read_text(encoding="utf-8-sig"))["products"]
    counts = Counter(s["hero"] for s in skins)
    return dict(checked_on=data["checked_on"], scope="Listed evidence files only, not complete project clearance",
                roster_count=len(data["characters"]), skin_count=len(skins), store_product_count=len(products),
                skins_per_slot={str(c["slot"]): counts[c["slot"]] for c in data["characters"]}, files=files)


def render(data, assets):
    esc = html.escape
    rows = []
    for c in data["characters"]:
        kind = c["status"]
        label = {"claim_found": "ЕСТЬ ЗАЯВЛЕНИЕ", "unresolved": "СТАТУС НЕЯСЕН", "cleared": "ЕСТЬ ОСНОВАНИЕ"}[kind]
        links = " ".join('<a href="#' + esc(s) + '">' + esc(s) + '</a>' for s in c["evidence"])
        r = c["replacement"]
        rows.append(f'''<article class="hero {kind}" data-name="{esc(c['name'])}">
<div class="portrait"><img loading="lazy" src="../{esc(c['preview'])}" alt="Превью из исходников: {esc(c['name'])}"></div>
<div class="body"><span class="tag">{label}</span><h3>{esc(c['name'])}</h3>
<p>{esc(c['finding'])} {links}</p><p class="action">{esc(c['next_action'])}</p>
<details><summary>Резерв: {esc(r['working_name'])} · {esc(r['role'])}</summary>
<p>{esc(r['design'])}</p><p>Пять направлений костюмов: {esc(r['skins'])}.</p>
<p class="muted">Рабочее имя и концепт. Модель ещё не изготовлена; проверка имени и сходства предстоит.</p></details>
<p class="file">slot {c['slot']} · {esc(c['model'])}</p></div></article>''')
    sources = "".join(f'<li id="{esc(s["id"])}"><strong>{esc(s["id"])} · '
                      f'<a href="{esc(s["url"], quote=True)}" target="_blank" rel="noopener noreferrer">{esc(s["title"])}</a>'
                      f'</strong><p>{esc(s["finding"])}</p></li>' for s in data["sources"])
    limitations = "".join('<li>' + esc(v) + '</li>' for v in data["limitations"])
    order = "".join('<li>' + esc(v) + '</li>' for v in data["implementation_order"])
    findings = "".join('<li><strong>' + esc(f["area"]) + '</strong><p>' + esc(f["detail"]) + '</p></li>'
                       for f in data["asset_findings"])
    claim_count = sum(c["status"] == "claim_found" for c in data["characters"])
    unknown_count = sum(c["status"] == "unresolved" for c in data["characters"])
    clear_count = sum(c["release_allowed"] for c in data["characters"])
    concept_path = ROOT / "Legal/replacement-five-concepts-20261015.png"
    concept = ('<figure><img class="concept" src="replacement-five-concepts-20261015.png" '
               'alt="Пять самостоятельных концептов"><figcaption>Эскизы первых пяти замен. '
               'Изображение создано imagegen по самостоятельному описанию, без исходных мем-картинок. '
               'Это концепт для Blender, а не готовая игровая модель и не правовое заключение.</figcaption></figure>') if concept_path.is_file() else ''
    return f'''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Brainrot Planet — аудит героев 15.10.2026</title>
<style>
*{{box-sizing:border-box}}body{{margin:0;color:#17243c;background:#edf2f7;font:16px/1.6 system-ui,sans-serif}}
main{{max-width:1190px;margin:auto;padding:38px 22px 70px}}h1{{font-size:clamp(29px,4vw,44px);line-height:1.15;max-width:850px}}
h2{{margin:36px 0 16px}}h3{{margin:7px 0;font-size:21px}}p{{margin:9px 0}}a{{color:#18577e}}li{{margin:12px 0}}
.muted,figcaption{{color:#526178;font-size:14px}}.intro,.summary,.panel{{background:white;border-radius:22px;padding:24px;margin:20px 0;border:1px solid #d6e0eb}}
.summary{{display:flex;gap:30px;flex-wrap:wrap}}.summary b{{font-size:34px;display:block;line-height:1.25}}
.hero{{display:grid;grid-template-columns:190px 1fr;background:white;border:1px solid #d6e0eb;border-left:5px solid #d99829;border-radius:20px;overflow:hidden;margin:16px 0}}
.hero[hidden]{{display:none}}.hero.claim_found{{border-left-color:#c85547}}.portrait{{background:#dfe7ed;display:flex;align-items:center;justify-content:center;padding:12px}}
.portrait img{{max-width:100%;max-height:230px;object-fit:contain}}.body{{padding:20px 24px}}.tag{{font-size:11px;letter-spacing:.09em;font-weight:800;color:#92561a}}
.claim_found .tag{{color:#ac3e37}}.action{{font-weight:650}}.file{{font:12px/1.6 ui-monospace,monospace;color:#63738a;overflow-wrap:anywhere}}
details{{padding:11px 14px;background:#edf4f5;border-radius:12px;margin-top:13px}}summary{{cursor:pointer;font-weight:650}}
input,select{{font:inherit;padding:10px 14px;border:1px solid #b8c9d9;border-radius:12px;background:white;max-width:100%}}
.filters{{display:flex;gap:12px;flex-wrap:wrap}}.concept{{display:block;width:100%;border-radius:18px}}figure{{margin:20px 0}}figcaption{{margin-top:9px}}
@media(max-width:650px){{.hero{{grid-template-columns:1fr}}.portrait{{height:170px}}.portrait img{{max-height:150px}}.body{{padding:18px}}}}
@media print{{body{{background:white}}.filters{{display:none}}.hero{{break-inside:avoid}}details{{display:block}}a{{color:inherit}}}}
</style><main><p class="muted">Внутренний материал разработки · {esc(data['checked_on'])} · исходная сборка 0.7.25</p>
<h1>Каких известных героев можно оставить в релизе</h1>
<div class="intro"><p>{esc(data['conclusion'])}</p><p><strong>{esc(data['policy'])}</strong></p>
<p class="muted">{esc(data['scope'])}</p></div>
<div class="summary"><div><b>{len(data['characters'])}</b>проверено по списку игры</div><div><b>{claim_count}</b>найдены прямые заявления</div>
<div><b>{unknown_count}</b>остаются неясными</div><div><b>{clear_count}</b>подтверждены для выпуска</div></div>
<h2>Состав и решения</h2><div class="filters"><input id="query" type="search" aria-label="Поиск героя" placeholder="Найти героя…">
<select id="status" aria-label="Статус"><option value="">Все 13</option><option value="claim_found">Есть заявление</option><option value="unresolved">Статус неясен</option></select></div>
<div id="heroes">{''.join(rows)}</div><p id="empty" hidden>Ничего не найдено.</p>
<h2>Самостоятельные замены: первая пятёрка</h2><p>Люмика → вихрь; Шелест → рывок; Мохокраб → корни; Битобот → удар; Пружик → прыжок.
Меняются сочетания форм, анатомия и характер движения. Новые имена пока рабочие.</p>{concept}
<h2>Что найдено в проекте</h2><div class="panel"><p><strong>{assets['skin_count']} скинов · {assets['store_product_count']} тестовых товаров · {len(assets['files'])} файлов в контрольном списке.</strong></p>
<ul>{findings}</ul><p><a href="character-asset-inventory-20261015.json">Контрольный список файлов с SHA-256</a> · <a href="character-rights-20261015.json">Полный реестр решений</a> · <a href="replacement-concept-prompt-20261015.txt">Описание для генерации эскиза</a></p></div>
<h2>План внедрения</h2><ol>{order}</ol><h2>Источники</h2><ol>{sources}</ol>
<h2>Границы проверки</h2><ul>{limitations}</ul><p class="muted">Статус «есть заявление» фиксирует опубликованную претензию на лицензирование/авторство. Он не означает подтверждение прав судом. Статус «неясен» не означает свободное использование.</p>
</main><script>
const filterQuery=document.getElementById('query'),statusSelect=document.getElementById('status');
function filter(){{let shown=0;for(const card of document.querySelectorAll('.hero')){{const visible=card.dataset.name.toLocaleLowerCase('ru').includes(filterQuery.value.toLocaleLowerCase('ru'))&&(!statusSelect.value||card.classList.contains(statusSelect.value));card.hidden=!visible;if(visible)shown++;}}document.getElementById('empty').hidden=shown>0;}}
filterQuery.addEventListener('input',filter);statusSelect.addEventListener('change',filter);
</script></html>'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", action="store_true", help="Generate local HTML and SHA-256 inventory")
    parser.add_argument("--check-release", action="store_true", help="Exit 2 if any current hero lacks a recorded basis")
    args = parser.parse_args()
    data = load()
    assets = inventory(data)
    if args.report:
        (ROOT / "Legal/character-asset-inventory-20261015.json").write_text(
            json.dumps(assets, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
        output = ROOT / "Legal/character-rights-audit-20261015.html"
        output.write_text(render(data, assets), encoding="utf-8", newline="\n")
        print("REPORT_WRITTEN", output)
    print("AUDIT_STRUCTURE_OK", "heroes=" + str(len(data["characters"])),
          "skins=" + str(assets["skin_count"]), "products=" + str(assets["store_product_count"]),
          "listed_files=" + str(len(assets["files"])))
    blocked = [c for c in data["characters"] if not c["release_allowed"]]
    print("RECORDED_DECISIONS", "cleared=" + str(len(data["characters"]) - len(blocked)), "unresolved_or_claimed=" + str(len(blocked)))
    if args.check_release and blocked:
        for c in blocked:
            print("NEEDS_EVIDENCE", c["slot"], c["key"], c["status"])
        print("RELEASE_CHECK_BLOCKED: evidence is incomplete; this is not a finding of infringement")
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
