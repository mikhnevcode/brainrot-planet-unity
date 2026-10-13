import asyncio
import struct
import unittest
from websockets.asyncio.client import connect
from websockets.asyncio.server import serve
from websockets.exceptions import InvalidStatus, ConnectionClosed
from gateway import Gateway


class GatewayTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.key = "unit-test-only-key-not-for-deployment"

        async def echo(reader, writer):
            try:
                while True:
                    size, = struct.unpack("<I", await reader.readexactly(4))
                    data = await reader.readexactly(size)
                    writer.write(struct.pack("<I", size) + data)
                    await writer.drain()
            except (asyncio.IncompleteReadError, ConnectionError):
                pass
            finally:
                writer.close()
                await writer.wait_closed()

        self.tcp = await asyncio.start_server(echo, "127.0.0.1", 0)
        self.gateway = Gateway(self.key, self.tcp.sockets[0].getsockname()[1])
        self.ws = await serve(self.gateway.handle, "127.0.0.1", 0,
                              process_request=self.gateway.authorize, max_size=2048, compression=None)
        self.url = "ws://127.0.0.1:%d/match" % self.ws.sockets[0].getsockname()[1]

    async def asyncTearDown(self):
        self.ws.close()
        await self.ws.wait_closed()
        self.tcp.close()
        await self.tcp.wait_closed()

    def client(self, key=None, url=None):
        return connect(url or self.url, additional_headers={"X-Brainrot-Test-Key": key or self.key}, proxy=None)

    async def test_reject_key_and_path(self):
        for key, url, status in [("wrong", self.url, 401), (self.key, self.url+"/bad", 404)]:
            with self.assertRaises(InvalidStatus) as error:
                async with self.client(key, url):
                    pass
            self.assertEqual(error.exception.response.status_code, status)

    async def test_binary_roundtrip_and_close(self):
        async with self.client() as ws:
            for data in [b"hello", bytes(range(256))*8]:
                await ws.send(data)
                self.assertEqual(await ws.recv(), data)

    async def test_text_rejected(self):
        async with self.client() as ws:
            await ws.send("not a binary command")
            with self.assertRaises(ConnectionClosed):
                await ws.recv()

    async def test_oversize_rejected(self):
        async with self.client() as ws:
            await ws.send(b"x"*2049)
            with self.assertRaises(ConnectionClosed):
                await ws.recv()

    async def test_room_capacity(self):
        peers=[]
        try:
            for _ in range(4):
                peers.append(await self.client())
            with self.assertRaises(InvalidStatus) as error:
                await self.client()
            self.assertEqual(error.exception.response.status_code,503)
        finally:
            await asyncio.gather(*(peer.close() for peer in peers))


if __name__ == "__main__":
    unittest.main()
