"""External WSS simulator test. Reads the private invitation locally; no secrets logged."""
import asyncio
import json
import os
from pathlib import Path
import sys
import uuid
from websockets.asyncio.client import connect
from websockets.exceptions import InvalidStatus


async def main():
    url=sys.argv[1]
    key=Path(os.environ.get('BRAINROT_KEY_FILE','runtime/test.key')).read_text().strip()
    async with connect(url,additional_headers={'X-Brainrot-Test-Key':key},max_size=131072,compression=None,proxy=None) as ws:
        token=uuid.uuid4().hex
        async def command(action,**fields):
            request=uuid.uuid4().hex
            q=dict(action=action,token=token,request=request);q.update(fields)
            await ws.send(json.dumps(q).encode());reply=json.loads(await asyncio.wait_for(ws.recv(),12))
            assert reply['request']==request and reply['mode']=='test'
            await asyncio.sleep(.6)
            return reply
        reply=await command('hello');assert len(reply['catalog']['products'])==99
        tx=uuid.uuid4().hex
        reply=await command('purchase',product='trail.stars',scenario='pending',tx=tx);assert 'trail:stars' not in reply['owned']
        reply=await command('approve',tx=tx);assert 'trail:stars' in reply['owned']
        reply=await command('purchase',product='trail.stars',scenario='success',tx=tx);assert len(reply['transactions'])==1
        reply=await command('refund',tx=tx);assert 'trail:stars' not in reply['owned']
        reply=await command('restore');assert reply['ok']
    try:
        async with connect(url,additional_headers={'X-Brainrot-Test-Key':'wrong-key'},proxy=None):
            raise AssertionError('Unauthenticated store accepted')
    except InvalidStatus as error:
        assert error.response.status_code==401
    print('STORE_WSS_SMOKE_PASS catalog99 pending approve duplicate refund restore unauthorized401 realMoney=0')


if __name__=='__main__':asyncio.run(main())
