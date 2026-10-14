import asyncio
import struct
import unittest

from websockets.asyncio.client import connect
from websockets.asyncio.server import serve
from websockets.exceptions import ConnectionClosed, InvalidStatus

from room_gateway import RoomGateway


class FixtureGateway(RoomGateway):
    async def start_backend(self, room):
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

        room.process = await asyncio.start_server(echo, "127.0.0.1", 0)
        room.port = room.process.sockets[0].getsockname()[1]

    async def stop_backend(self, room):
        if room.process is not None:
            room.process.close()
            await room.process.wait_closed()


class RoomGatewayTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.gateway = FixtureGateway(max_rooms=2, idle_seconds=.05)
        self.server = await serve(self.gateway.handle, "127.0.0.1", 0,
                                  process_request=self.gateway.authorize,
                                  max_size=2048, compression=None)
        self.base = "ws://127.0.0.1:%d/match" % self.server.sockets[0].getsockname()[1]
        self.peers = []

    async def asyncTearDown(self):
        await asyncio.gather(*(peer.close() for peer in self.peers),
                             return_exceptions=True)
        self.server.close()
        await self.server.wait_closed()
        await self.gateway.close()

    async def open(self, route):
        peer = await connect(self.base + route, proxy=None)
        self.peers.append(peer)
        info = await asyncio.wait_for(peer.recv(), 2)
        self.assertEqual(info[0], 7)
        self.assertEqual(info[1], len(info)-2)
        return peer, info[2:].decode("ascii")

    async def test_quick_match_pools_four_then_opens_next_room(self):
        players = [await self.open("/quick") for _ in range(5)]
        self.assertEqual([code for _, code in players], [""]*5)
        self.assertEqual(len(self.gateway.rooms), 2)
        self.assertEqual(sorted(room.clients for room in self.gateway.rooms.values()), [1, 4])
        for index, (peer, _) in enumerate(players):
            packet = bytes((9, index))
            await peer.send(packet)
            self.assertEqual(await asyncio.wait_for(peer.recv(), 2), packet)

    async def test_private_code_is_unique_joinable_and_closes_on_start(self):
        owner, code = await self.open("/create")
        self.assertEqual(len(code), 6)
        self.assertIsNone(self.gateway.rooms[code].ready)
        friend, joined_code = await self.open("/join/" + code.lower())
        self.assertEqual(code, joined_code)
        await asyncio.wait_for(self.gateway.rooms[code].ready, 2)
        second_owner, second_code = await self.open("/create")
        self.assertNotEqual(code, second_code)
        self.assertEqual(len(self.gateway.rooms), 2)
        await friend.send(b"\x09friend")
        self.assertEqual(await friend.recv(), b"\x09friend")
        with self.assertRaises(InvalidStatus) as error:
            await connect(self.base + "/join/AAAAAA", proxy=None)
        self.assertEqual(error.exception.response.status_code, 404)
        self.gateway.rooms[code].started = True
        with self.assertRaises(InvalidStatus) as error:
            await connect(self.base + "/join/" + code, proxy=None)
        self.assertEqual(error.exception.response.status_code, 404)
        self.assertIsNotNone(owner)
        self.assertIsNotNone(second_owner)

    async def test_limit_route_validation_and_idle_reap(self):
        first, _ = await self.open("/create")
        second, _ = await self.open("/create")
        with self.assertRaises(InvalidStatus) as error:
            await connect(self.base + "/create", proxy=None)
        self.assertEqual(error.exception.response.status_code, 503)
        with self.assertRaises(InvalidStatus) as error:
            await connect(self.base + "/anything", proxy=None)
        self.assertEqual(error.exception.response.status_code, 404)
        await first.close()
        await second.close()
        await asyncio.sleep(.15)
        self.assertEqual(len(self.gateway.rooms), 0)
        replacement, code = await self.open("/create")
        self.assertEqual(len(code), 6)
        self.assertIsNotNone(replacement)

    async def test_text_is_rejected(self):
        peer, _ = await self.open("/quick")
        await peer.send("not binary")
        with self.assertRaises(ConnectionClosed):
            await peer.recv()


if __name__ == "__main__":
    unittest.main()
