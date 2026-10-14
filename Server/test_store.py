import json
from pathlib import Path
import tempfile
import unittest
import uuid
from store_service import StoreService

CATALOG=Path(__file__).resolve().parent.parent/'Content/Store/catalog-test-v1.json'


class StoreTests(unittest.TestCase):
    def setUp(self):
        self.folder=tempfile.TemporaryDirectory()
        self.database=Path(self.folder.name)/'store.sqlite3'
        self.store=StoreService(self.database,CATALOG)
        self.token=uuid.uuid4().hex
        self.command('hello')

    def tearDown(self):
        self.store.close();self.folder.cleanup()

    def command(self,action,**fields):
        q=dict(action=action,token=self.token,request=uuid.uuid4().hex);q.update(fields)
        return json.loads(self.store.handle(json.dumps(q).encode()))

    def buy(self,product,scenario='success',tx=None):
        return self.command('purchase',product=product,scenario=scenario,tx=tx or uuid.uuid4().hex)

    def test_catalog_all_99_products(self):
        self.assertEqual(len(self.store.products),99)
        self.assertEqual(len(self.command('restore')['owned']),13)
        for p in self.store.products.values():
            reply=self.buy(p['id']);self.assertTrue(reply['ok'])
            self.assertTrue(set(p['grants'])<=set(reply['owned']))

    def test_cancel_error_pending_do_not_grant(self):
        for status in ['cancel','error','pending']:
            reply=self.buy('trail.stars',status)
            self.assertEqual(reply['status'],status);self.assertNotIn('trail:stars',reply['owned'])

    def test_idempotent_and_wrong_sku(self):
        tx=uuid.uuid4().hex
        first=self.buy('trail.stars',tx=tx)
        repeat=self.buy('trail.stars','cancel',tx=tx)
        self.assertEqual(first['owned'],repeat['owned'])
        self.assertEqual(len(repeat['transactions']),1)
        self.assertFalse(self.buy('trail.petals',tx=tx)['ok'])

    def test_pending_approve_then_refund(self):
        tx=uuid.uuid4().hex;self.buy('trail.stars','pending',tx)
        reply=self.command('approve',tx=tx);self.assertIn('trail:stars',reply['owned'])
        reply=self.command('refund',tx=tx);self.assertNotIn('trail:stars',reply['owned'])
        self.assertEqual(self.command('approve',tx=tx)['status'],'refunded')
        self.assertEqual(self.command('refund',tx=tx)['status'],'refunded')

    def test_overlapping_bundle_refund(self):
        tx=uuid.uuid4().hex;self.buy('trail.stars',tx=tx);self.buy('bundle.space')
        self.assertIn('trail:stars',self.command('refund',tx=tx)['owned'])

    def test_account_isolation_and_restart_restore(self):
        tx=uuid.uuid4().hex;self.buy('trail.stars',tx=tx)
        self.assertFalse(self.command('restore',token=uuid.uuid4().hex)['ok'])
        self.command('hello',token='b'*32)
        self.assertNotIn('trail:stars',self.command('restore',token='b'*32)['owned'])
        self.assertFalse(self.command('refund',token='b'*32,tx=tx)['ok'])
        self.store.close();self.store=StoreService(self.database,CATALOG)
        self.assertIn('trail:stars',self.command('restore')['owned'])

    def test_no_client_price_or_receipt_authority(self):
        self.assertFalse(self.buy('damage.boost')['ok'])
        self.assertFalse(self.buy('trail.stars','paid_real_money')['ok'])
        self.assertFalse(self.command('production_receipt',receipt='anything')['ok'])
        # Amount supplied by the client is irrelevant to this zero-money simulator.
        self.assertTrue(self.command('purchase',product='trail.stars',scenario='success',tx=uuid.uuid4().hex,amount=-999)['ok'])

    def test_malformed_frames(self):
        for raw in [b'',b'x'*2049,b'[]',b'{',b'\xff',b'{"token":12}',b'{"token":"'+b'a'*32+b'","request":12}']:
            self.assertFalse(json.loads(self.store.handle(raw))['ok'])

    def test_no_ledger_truncation_grant_loss(self):
        self.buy('trail.stars')
        for _ in range(499):self.buy('trail.bubbles','cancel')
        self.assertFalse(self.buy('trail.petals')['ok'])
        self.assertIn('trail:stars',self.command('restore')['owned'])


if __name__=='__main__':unittest.main()
