"""Private single-room WSS bridge. TLS is terminated by a dedicated tunnel.
Both listeners MUST stay on loopback; never expose the raw Unity TCP port.
Default match mode has no accounts/payments/persistent game data. Optional
private /store-test adds a separate zero-money anonymous test ledger only.
"""
import asyncio
import contextlib
import hmac
import logging
import os
from pathlib import Path
import secrets
import struct
import time
from http import HTTPStatus

from websockets.asyncio.server import serve

MAX_PACKET = 196608


class Gateway:
    def __init__(self, key, tcp_port=27766, store=None):
        if len(key) < 24:
            raise ValueError("Test key must have at least 24 characters")
        self.key = key
        self.tcp_port = tcp_port
        self.active = 0
        self.store = store
        self.store_active = 0

    def authorize(self, connection, request):
        if request.path != "/match" and not (request.path == "/store-test" and self.store is not None):
            return connection.respond(HTTPStatus.NOT_FOUND, "Not found\n")
        keys = request.headers.get_all("X-Brainrot-Test-Key")
        if len(keys) != 1 or not hmac.compare_digest(keys[0].encode(), self.key.encode()):
            return connection.respond(HTTPStatus.UNAUTHORIZED, "Private test room\n")
        if (self.store_active if request.path == "/store-test" else self.active) >= 4:
            return connection.respond(HTTPStatus.SERVICE_UNAVAILABLE, "Room full\n")

    async def handle(self, websocket):
        if websocket.request.path == "/store-test" and self.store is not None:
            await self.handle_store(websocket)
            return
        # Re-check after handshake: several handshakes may finish in one event loop turn.
        if self.active >= 4:
            await websocket.close(1013, "Room full")
            return
        self.active += 1
        writer = None
        tasks = []
        try:
            reader, writer = await asyncio.wait_for(asyncio.open_connection("127.0.0.1", self.tcp_port), 3)

            async def incoming():
                tokens, previous = 50.0, time.monotonic()
                async for data in websocket:
                    now = time.monotonic()
                    tokens = min(50.0, tokens + (now - previous) * 30) - 1
                    previous = now
                    if tokens < 0 or not isinstance(data, bytes) or not 1 <= len(data) <= 2048:
                        await websocket.close(1008, "Invalid client traffic")
                        return
                    writer.write(struct.pack("<I", len(data)) + data)
                    await asyncio.wait_for(writer.drain(), 5)

            async def outgoing():
                while True:
                    header = await asyncio.wait_for(reader.readexactly(4), 15)
                    size, = struct.unpack("<I", header)
                    if not 1 <= size <= MAX_PACKET:
                        raise ValueError("Invalid server frame")
                    data = await asyncio.wait_for(reader.readexactly(size), 5)
                    await asyncio.wait_for(websocket.send(data), 5)

            tasks = [asyncio.create_task(incoming()), asyncio.create_task(outgoing())]
            done, _ = await asyncio.wait(tasks, return_when=asyncio.FIRST_COMPLETED)
            for task in done:
                task.result()
        except Exception as error:
            # Never log request headers, keys or packet payloads.
            logging.info("Peer ended: %s", type(error).__name__)
        finally:
            for task in tasks:
                task.cancel()
            await asyncio.gather(*tasks, return_exceptions=True)
            if writer is not None:
                writer.close()
                with contextlib.suppress(Exception):
                    await writer.wait_closed()
            self.active -= 1
            await websocket.close()

    async def handle_store(self, websocket):
        if self.store_active>=4:
            await websocket.close(1013,'Test store full');return
        self.store_active+=1
        tokens,previous=8.0,time.monotonic()
        try:
            async for data in websocket:
                now=time.monotonic();tokens=min(8.0,tokens+(now-previous)*2)-1;previous=now
                if tokens<0 or not isinstance(data,bytes) or not 1<=len(data)<=2048:
                    await websocket.close(1008,'Invalid test store traffic');return
                await websocket.send(self.store.handle(data))
        except Exception as error:
            logging.info('Test store peer ended: %s',type(error).__name__)
        finally:
            self.store_active-=1


async def main():
    key_path = Path(os.environ.get("BRAINROT_KEY_FILE", "runtime/test.key"))
    if not key_path.exists():
        key_path.parent.mkdir(parents=True, exist_ok=True)
        with open(key_path, "x", opener=lambda path, flags: os.open(path, flags, 0o600)) as stream:
            stream.write(secrets.token_urlsafe(24))
    store=None
    if os.environ.get('BRAINROT_STORE_TEST')=='1':
        from store_service import StoreService
        catalog=Path(os.environ.get('BRAINROT_STORE_CATALOG','catalog-test-v1.json'))
        store=StoreService(key_path.parent/'store-test.sqlite3',catalog)
    gateway = Gateway(key_path.read_text().strip(),tcp_port=int(os.environ.get('BRAINROT_TCP_PORT','27766')),store=store)
    port=int(os.environ.get('BRAINROT_GATEWAY_PORT','27767'))
    async with serve(gateway.handle, "127.0.0.1", port, process_request=gateway.authorize,
                     origins=[None], compression=None, max_size=2048, max_queue=8,
                     open_timeout=5, ping_interval=5, ping_timeout=10, close_timeout=2,
                     server_header=None) as server:
        logging.info("MATCH_GATEWAY_READY loopbackPort=%d storeTest=%s",port,store is not None)
        await server.serve_forever()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(message)s")
    asyncio.run(main())
