"""Build cosmetic-only TEST catalog from the immutable wardrobe manifest."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def build(original=False):
    source='Content/OriginalCast/Skins/catalog-original26-v1.json' if original else 'Content/Skins/catalog-cast-v1.json'
    manifest = json.loads((ROOT / source).read_text(encoding='utf-8'))
    products, free = [], []
    by_hero = {}
    for skin in manifest['skins']:
        hero = skin['hero']
        by_hero.setdefault(hero, []).append(skin['id'])
        products.append(dict(id='skin.'+skin['id'], title=skin['title'], kind='skin', hero=hero,
                             priceTest=149, grants=['skin:'+skin['id']], skin=skin['id']))
    extras = {
        'trail': [('bubbles','Пузырьковый след'),('petals','Лепестковый след'),('stars','Звёздный след')],
        'spawn': [('ocean','Океанское появление'),('bloom','Цветочное появление'),('space','Космическое появление')],
        'dance': [('disco','Победное диско'),('robot','Танец робота'),('sway','Праздничный кач')],
        'stage': [('coral','Коралловый подиум'),('garden','Цветочная сцена'),('orbital','Орбитальная сцена')],
        'emote': [('hello','Привет!'),('great','Отлично!'),('again','Ещё матч!')],
        'profile': [('coral','Коралловая карточка'),('bloom','Цветочная карточка'),('space','Космическая карточка')],
    }
    for kind, values in extras.items():
        for key, title in values:
            products.append(dict(id=kind+'.'+key, title=title, kind=kind, hero=-1, priceTest=79,
                                 grants=[kind+':'+key], skin=''))
    for hero, ids in sorted(by_hero.items()):
        free.append('skin:'+ids[0])
        products.append(dict(id=('bundle.original26.hero' if original else 'bundle.hero')+str(hero), title='Полный гардероб героя '+str(hero+1),
                             kind='bundle',hero=hero,priceTest=399,grants=['skin:'+x for x in ids],skin=ids[-1]))
    themes = [('ocean','bubbles','coral','disco','hello'),('bloom','petals','garden','sway','great'),('space','stars','orbital','robot','again')]
    for theme, trail, stage, dance, emote in themes:
        products.append(dict(id='bundle.'+theme,title={'ocean':'Океанский комплект','bloom':'Цветочный комплект','space':'Космический комплект'}[theme],
                             kind='bundle',hero=-1,priceTest=299,skin='',grants=['trail:'+trail,'spawn:'+theme,'stage:'+stage,'dance:'+dance,'emote:'+emote,'profile:'+theme.replace('ocean','coral')]))
    catalog = dict(schema=1, revision=1, mode='test', products=products, free=free)
    target = ROOT / ('Content/OriginalCast/Store/catalog-original26-test-v1.json' if original else 'Content/Store/catalog-test-v1.json')
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(catalog, ensure_ascii=False, indent=2)+'\n', encoding='utf-8', newline='\n')
    print('STORE_CATALOG_BUILT products=%d free=%d' % (len(products),len(free)))


if __name__ == '__main__':
    import sys
    build('--original26' in sys.argv[1:])
