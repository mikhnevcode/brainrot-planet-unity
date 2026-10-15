"""Exercise public quick matchmaking and a private invite over a real WSS tunnel.

Usage: python room_match_smoke.py wss://host/match
Creates two isolated short-lived rooms, then disconnects all test peers.
"""
import asyncio
import hashlib
import struct
import sys
import time

from websockets.asyncio.client import connect


def dotnet_string(value):
    encoded = value.encode("utf-8")
    assert len(encoded) < 128
    return bytes((len(encoded),)) + encoded


async def join(base, route):
    ws = await connect(base.rstrip("/") + route, max_size=196608,
                       compression=None, proxy=None)
    packet = await asyncio.wait_for(ws.recv(), 10)
    assert packet[0] == 7 and packet[1] == len(packet)-2, "missing RoomInfo"
    code = packet[2:].decode("ascii")
    await ws.send(struct.pack("<BiB", 1, 9, 0) + b"\0" +
                  dotnet_string("QA-PLAYER"))
    return ws, code


async def observe(ws, seconds=18):
    team, epoch, snapshots, planet = 0, 0, set(), None
    sequence = 0

    async def inputs():
        nonlocal sequence
        while True:
            sequence += 1
            await ws.send(struct.pack("<BIIfffB", 4, epoch, sequence, 0, 0, 0, 0))
            await asyncio.sleep(.05)

    sender = asyncio.create_task(inputs())
    try:
        until = time.monotonic() + seconds
        while time.monotonic() < until:
            packet = await asyncio.wait_for(ws.recv(), 15)
            if packet[0] == 2:
                team = packet[1]
            elif packet[0] == 3:
                epoch, team = struct.unpack_from("<IB", packet, 1)
                planet = struct.unpack_from("<Bi", packet, 7)
            elif packet[0] == 5:
                received, = struct.unpack_from("<I", packet, 1)
                assert received == epoch and epoch > 0
                snapshots.add(hashlib.sha256(packet).digest())
    finally:
        sender.cancel()
        await asyncio.gather(sender, return_exceptions=True)
    return team, epoch, snapshots, planet


async def main(base):
    quick_a, quick_code = await join(base, "/quick")
    quick_b, second_code = await join(base, "/quick")
    private_a, private_code = await join(base, "/create")
    private_b, friend_code = await join(base, "/join/" + private_code)
    peers = (quick_a, quick_b, private_a, private_b)
    try:
        assert quick_code == second_code == ""
        assert len(private_code) == 6 and private_code == friend_code
        results = await asyncio.gather(*(observe(peer) for peer in peers))
        for left, right in ((0, 1), (2, 3)):
            first, second = results[left], results[right]
            assert first[0] != second[0] and first[1] == second[1] > 0
            assert first[3] == second[3] and first[3] is not None
            common = first[2] & second[2]
            assert len(common) >= 50, ("too few shared snapshots room=%d "
                                       "first=%d second=%d common=%d" %
                                       (left, len(first[2]), len(second[2]), len(common)))
        assert results[0][3] != results[2][3], "private and quick seeds match"
        print("ROOM_WSS_PASS quickTeams=%s privateTeams=%s privateCode=%s" %
              ((results[0][0], results[1][0]),
               (results[2][0], results[3][0]), private_code))
    finally:
        await asyncio.gather(*(peer.close() for peer in peers),
                             return_exceptions=True)


if __name__ == "__main__":
    asyncio.run(main(sys.argv[1]))
