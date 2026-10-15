"""Build the shareable 26-character gallery without changing source images."""

import argparse
import hashlib
import html
import json
import re
import shutil
import struct
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SERIES = [ROOT / "Characters" / f"OriginalConcepts_20261015_v{n}" for n in (1, 2)]
PUBLIC_URL = "https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/heroes/"

EXTRA_STYLE = """
.toolbar{display:flex;flex-wrap:wrap;align-items:center;gap:10px;margin-top:24px}
.toolbar button,.share-row button{border:1px solid #b9d5e0;border-radius:999px;padding:11px 17px;background:#fff;color:#193c55;font:inherit;font-size:14px;font-weight:700;cursor:pointer;min-height:44px}
.toolbar button[aria-pressed=true]{background:#176f90;border-color:#176f90;color:white}
.toolbar .share-page{margin-left:auto;background:#ffdf9b;border-color:#f1c66f}
.card[hidden]{display:none}.status{color:#527183;font-size:14px;min-height:22px;line-height:1.5}
.share-row{display:flex;flex-wrap:wrap;gap:12px;align-items:center;padding:0 18px 18px}.share-row a{overflow-wrap:anywhere;font-size:13px;flex:1;min-width:180px}
.new-badge{display:inline-block;margin-left:8px;font-size:10px;background:#d9f8e9;color:#197753;padding:3px 8px;border-radius:999px;letter-spacing:.04em}
@media(max-width:560px){.toolbar .share-page{margin-left:0}.toolbar{gap:8px}.toolbar button{padding:10px 13px}}
"""

SCRIPT = """
const cards=[...document.querySelectorAll('.card')],viewer=document.querySelector('dialog');
const filters=[...document.querySelectorAll('[data-filter]')],status=document.querySelector('#share-status');
let index=0;
function setFilter(value){
 cards.forEach(card=>card.hidden=value!=='all'&&card.dataset.series!==value);
 filters.forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.filter===value)));
 document.querySelector('#visible-count').textContent=String(cards.filter(card=>!card.hidden).length);
}
function show(value,updateUrl=true){
 index=(value+cards.length)%cards.length;
 const card=cards[index],source=card.querySelector('img'),target=viewer.querySelector('img');
 target.src=source.getAttribute('src');target.alt=source.alt;
 document.querySelector('#viewer-title').textContent=card.dataset.number.padStart(2,'0')+' · '+card.querySelector('strong').textContent;
 viewer.querySelector('.viewer-desc').textContent=card.querySelector('.description').textContent;
 const url=new URL(location.href);url.hash=card.id;
 const link=document.querySelector('#hero-link');link.href=url.href;link.textContent='Ссылка на героя № '+card.dataset.number;
 if(updateUrl)history.replaceState(null,'',url.href);
 if(!viewer.open)viewer.showModal();
}
function step(direction){
 const visible=cards.map((card,i)=>card.hidden?-1:i).filter(i=>i>=0);
 const position=visible.indexOf(index);show(visible[(position+direction+visible.length)%visible.length]);
}
function fromHash(){
 const i=cards.findIndex(card=>'#'+card.id===location.hash);
 if(i>=0){if(cards[i].hidden)setFilter('all');show(i,false)}
}
function announce(message){status.textContent=message;document.querySelector('#viewer-share-status').textContent=message}
async function share(url,title){
 if(navigator.share){try{await navigator.share({title,text:'Какие герои нравятся? Пришли мне их номера.',url});return}catch(error){if(error.name==='AbortError')return}}
 try{await navigator.clipboard.writeText(url);announce('Ссылка скопирована — можно отправить друзьям.')}
 catch(error){announce('Скопируйте ссылку из адресной строки браузера.')}
}
cards.forEach((card,i)=>card.addEventListener('click',()=>show(i)));
filters.forEach(button=>button.addEventListener('click',()=>setFilter(button.dataset.filter)));
document.querySelector('#previous').addEventListener('click',()=>step(-1));
document.querySelector('#next').addEventListener('click',()=>step(1));
document.querySelector('#close').addEventListener('click',()=>viewer.close());
viewer.addEventListener('close',()=>history.replaceState(null,'',location.pathname+location.search));
viewer.addEventListener('click',event=>{if(event.target===viewer)viewer.close()});
document.querySelector('#share-page').addEventListener('click',()=>{const url=new URL(location.href);url.hash='';share(url.href,document.title)});
document.querySelector('#share-hero').addEventListener('click',()=>share(document.querySelector('#hero-link').href,document.querySelector('#viewer-title').textContent));
document.addEventListener('keydown',event=>{if(!viewer.open)return;if(event.key==='ArrowRight'){event.preventDefault();step(1)}if(event.key==='ArrowLeft'){event.preventDefault();step(-1)}});
window.addEventListener('hashchange',fromHash);fromHash();
"""


def build(output):
    first_html = (SERIES[0] / "gallery.html").read_text(encoding="utf-8")
    first_descriptions = re.findall(r'<span class="description">(.*?)</span>', first_html)
    assert len(first_descriptions) == 13, "Unexpected first-series gallery structure"
    characters = []
    for number, directory in enumerate(SERIES, 1):
        manifest = json.loads((directory / "manifest.json").read_text(encoding="utf-8"))
        prompts = json.loads((directory / "prompts.json").read_text(encoding="utf-8"))
        assert len(manifest["characters"]) == len(prompts["characters"]) == 13
        for i, entry in enumerate(manifest["characters"]):
            source = directory / entry["filename"]
            assert source.parent == directory and source.is_file()
            raw = source.read_bytes()
            assert raw[:8] == b"\x89PNG\r\n\x1a\n"
            assert struct.unpack(">II", raw[16:24]) == (entry["width"], entry["height"])
            assert hashlib.sha256(raw).hexdigest() == entry["sha256"], source
            description = html.unescape(first_descriptions[i]) if number == 1 else prompts["characters"][i]["description"]
            characters.append({**entry, "series": number, "number": len(characters) + 1, "description": description, "source": source})
    assert len({entry["id"] for entry in characters}) == 26
    assets = output / "assets"
    assets.mkdir(parents=True, exist_ok=True)
    cards = []
    for entry in characters:
        shutil.copyfile(entry["source"], assets / entry["filename"])
        n = entry["number"]
        name, description = html.escape(entry["name"]), html.escape(entry["description"])
        badge = '<span class="new-badge">НОВЫЙ</span>' if entry["series"] == 2 else ""
        cards.append(f'''<button class="card" type="button" id="hero-{n:02d}" data-number="{n}" data-series="{entry['series']}" aria-label="Открыть героя {n}: {name}">
<img src="assets/{entry['filename']}" alt="{name} — концепт героя" width="{entry['width']}" height="{entry['height']}" loading="{'eager' if n <= 3 else 'lazy'}" decoding="async">
<span class="card-copy"><span class="number">{n:02d}{badge}</span><strong>{name}</strong><span class="description">{description}</span></span></button>''')
    page = first_html.replace("13 новых героев", "26 новых героев").replace("Концепты v1", "Две серии концептов")
    metadata = f'''<meta name="robots" content="noindex, nofollow">
<meta name="description" content="26 необычных героев для Brainrot Planet. Выберите любимых и пришлите их номера.">
<meta property="og:title" content="26 новых героев — Brainrot Planet">
<meta property="og:description" content="Пельмолёт, Лампочмель, Клубкомот и ещё 23 необычных героя. Кто вам нравится?">
<meta property="og:type" content="website"><meta property="og:url" content="{PUBLIC_URL}">
<meta property="og:image" content="{PUBLIC_URL}assets/02-pelmolyot.png">
<link rel="icon" href="data:,">
'''
    page = page.replace("<style>", metadata + "<style>").replace("</style>", EXTRA_STYLE + "</style>")
    page = page.replace("Имена и способности пока рабочие.", "Имена и способности пока рабочие. Выберите любимых и пришлите автору номера: например, 02, 16, 22.")
    toolbar = '''<nav class="toolbar" aria-label="Выбор серии"><button type="button" data-filter="all" aria-pressed="true">Все 26</button><button type="button" data-filter="2" aria-pressed="false">Новые 13</button><button type="button" data-filter="1" aria-pressed="false">Первые 13</button><button type="button" class="share-page" id="share-page">Поделиться страницей</button></nav><p class="status">Показано: <span id="visible-count">26</span>. Нажмите на героя для увеличения.</p><p class="status" id="share-status" role="status"></p>'''
    page = page.replace("</header>", "</header>" + toolbar)
    page = re.sub(r'(<section class="grid"[^>]*>).*?(</section>)', lambda match: match[1] + "\n" + "\n".join(cards) + "\n" + match[2], page, flags=re.S)
    page = re.sub(r"<footer>.*?</footer>", "<footer>Концепт-арты созданы встроенным генератором изображений. Это обсуждение будущих героев, не готовая игра. Страница не использует формы, рекламные счётчики или сторонние шрифты.</footer>", page, flags=re.S)
    page = page.replace('<p class="viewer-desc"></p>', '<p class="viewer-desc"></p><div class="share-row"><button type="button" id="share-hero">Поделиться героем</button><a id="hero-link" href="#">Ссылка на героя</a><p class="status" id="viewer-share-status" role="status"></p></div>')
    page = re.sub(r"<script>.*?</script>", "<script>" + SCRIPT + "</script>", page, flags=re.S)
    (output / "index.html").write_text(page.rstrip() + "\n", encoding="utf-8", newline="\n")
    public_manifest = {"count": 26, "url": PUBLIC_URL, "characters": [{key: value for key, value in entry.items() if key != "source"} for entry in characters]}
    (output / "gallery-manifest.json").write_text(json.dumps(public_manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"GALLERY_OK heroes=26 first=13 new=13 image_hashes=26 output={output}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "Builds" / "HeroGallery_20261015_v2")
    build(parser.parse_args().output.resolve())
