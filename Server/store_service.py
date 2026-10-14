"""Private TEST purchase ledger. No money, payment keys, or real receipts accepted.

Production must use verified provider receipts and real account authentication.
Do NOT expose this sandbox as a production payment/entitlement service.
"""
import hashlib
import json
import re
import sqlite3

TOKEN = re.compile(r'[a-f0-9]{32}\Z')
IDENT = re.compile(r'[a-z0-9_.-]{1,80}\Z')
KINDS = {'skin','trail','spawn','dance','stage','emote','profile','bundle'}


class StoreService:
    def __init__(self, db_path, catalog_path):
        self.catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
        if self.catalog.get('schema') != 1 or self.catalog.get('mode') != 'test':
            raise ValueError('Test catalog only')
        self.products = {p['id']:p for p in self.catalog['products']}
        if len(self.products) != len(self.catalog['products']) or len(self.products)>200:
            raise ValueError('Catalog count')
        for p in self.products.values():
            if not IDENT.fullmatch(p['id']) or p['kind'] not in KINDS or not 1<=len(p['grants'])<=32:
                raise ValueError('Catalog product')
            for grant in p['grants']:
                kind, _, value = grant.partition(':')
                if kind not in KINDS-{'bundle'} or not IDENT.fullmatch(value):
                    raise ValueError('Cosmetics only')
        self.db = sqlite3.connect(str(db_path))
        self.db.execute('PRAGMA journal_mode=WAL')
        self.db.execute('CREATE TABLE IF NOT EXISTS accounts (id TEXT PRIMARY KEY)')
        self.db.execute('CREATE TABLE IF NOT EXISTS purchases (account TEXT, tx TEXT, product TEXT, status TEXT, PRIMARY KEY(account,tx))')
        self.db.commit()

    def close(self):
        self.db.close()

    def handle(self, raw):
        request=''
        try:
            if not isinstance(raw,bytes) or not 1<=len(raw)<=2048:
                raise ValueError('frame')
            q=json.loads(raw.decode('utf-8'))
            if not isinstance(q,dict) or not TOKEN.fullmatch(q.get('token','')):
                raise ValueError('credential')
            request=q.get('request','')
            if not TOKEN.fullmatch(request):
                raise ValueError('request')
            account=hashlib.sha256(q['token'].encode('ascii')).hexdigest()
            action=q.get('action')
            if action=='hello':
                count=self.db.execute('SELECT COUNT(*) FROM accounts').fetchone()[0]
                if count>=500 and not self.db.execute('SELECT 1 FROM accounts WHERE id=?',(account,)).fetchone():
                    raise ValueError('capacity')
                with self.db:self.db.execute('INSERT OR IGNORE INTO accounts VALUES(?)',(account,))
            elif not self.db.execute('SELECT 1 FROM accounts WHERE id=?',(account,)).fetchone():
                raise ValueError('account')
            status='restored'
            tx=q.get('tx','')
            if action=='purchase':
                product=q.get('product','');scenario=q.get('scenario','success')
                if product not in self.products or not TOKEN.fullmatch(tx) or scenario not in {'success','cancel','error','pending'}:
                    raise ValueError('purchase')
                existing=self.db.execute('SELECT product,status FROM purchases WHERE account=? AND tx=?',(account,tx)).fetchone()
                if existing:
                    if existing[0]!=product:raise ValueError('transaction_product')
                    status=existing[1]
                else:
                    if self.db.execute('SELECT COUNT(*) FROM purchases WHERE account=?',(account,)).fetchone()[0]>=500:
                        raise ValueError('transaction_capacity')
                    with self.db:self.db.execute('INSERT INTO purchases VALUES(?,?,?,?)',(account,tx,product,scenario))
                    status=scenario
            elif action in {'approve','refund'}:
                if not TOKEN.fullmatch(tx):raise ValueError('transaction')
                row=self.db.execute('SELECT status FROM purchases WHERE account=? AND tx=?',(account,tx)).fetchone()
                if row is None:raise ValueError('missing_transaction')
                # Sandbox-only controls, never a substitute for provider notifications.
                status='success' if action=='approve' and row[0]=='pending' else 'refunded' if action=='refund' and row[0]=='success' else row[0]
                with self.db:self.db.execute('UPDATE purchases SET status=? WHERE account=? AND tx=?',(status,account,tx))
            elif action not in {'hello','restore'}:
                raise ValueError('action')
            owned=set(self.catalog['free'])
            rows=self.db.execute('SELECT tx,product,status FROM purchases WHERE account=? ORDER BY rowid DESC LIMIT 500',(account,)).fetchall()
            for _,product,state in rows:
                if state=='success':owned.update(self.products.get(product,{}).get('grants',[]))
            result=dict(schema=1,mode='test',ok=True,request=request,status=status,owned=sorted(owned),
                        transactions=[dict(tx=t,product=p,status=s) for t,p,s in rows],catalog=self.catalog)
            return json.dumps(result,ensure_ascii=False,separators=(',',':')).encode('utf-8')
        except (ValueError,TypeError,KeyError,UnicodeError):
            # Do not echo tokens, supplied payloads or server internals.
            return json.dumps(dict(schema=1,mode='test',ok=False,status='invalid_request',request=request if isinstance(request,str) and TOKEN.fullmatch(request) else '')).encode('utf-8')
