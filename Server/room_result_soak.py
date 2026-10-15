"""Hold a real private match's result for 25 seconds, then check a replay.

Run only against the isolated test gateway: python room_result_soak.py WSS_URL.
This catches post-result rigidbody drift that short join smoke tests miss.
"""
import asyncio
import math
import struct
import sys
import time

from room_match_smoke import join


async def watch(ws):
    epoch = 0
    sequence = 0
    ended_at = None
    frozen = None
    frames = 0
    initial_planet = None
    replay_requested = False
    began = time.monotonic()

    async def inputs():
        nonlocal sequence
        while True:
            sequence += 1
            await ws.send(struct.pack("<BIIfffB", 4, epoch, sequence, 0, 0, 0, 0))
            await asyncio.sleep(.05)

    sender = asyncio.create_task(inputs())
    try:
        while time.monotonic() - began < 215:
            packet = await asyncio.wait_for(ws.recv(), 15)
            if packet[0] == 3:
                received, = struct.unpack_from("<I", packet, 1)
                planet = packet[7]
                if epoch:
                    assert replay_requested and received > epoch
                    assert planet == (initial_planet + 1) % 10
                    return frames, initial_planet, planet
                epoch = received
                initial_planet = planet
            elif packet[0] == 5:
                received, ended, winner, remaining = struct.unpack_from("<I?Bf", packet, 1)
                assert received == epoch and math.isfinite(remaining)
                poses = []
                for team in range(4):
                    offset = 11 + team * 62
                    motion = struct.unpack_from("<10f", packet, offset)
                    stats = struct.unpack_from("<5f", packet, offset + 42)
                    assert all(math.isfinite(x) for x in motion + stats)
                    assert max(abs(x) for x in motion[:3]) <= 100
                    assert .8 <= sum(x*x for x in motion[3:7]) <= 1.2
                    poses.append(motion[:7])
                if ended:
                    assert winner in (1, 2, 3, 4)
                    if ended_at is None:
                        ended_at = time.monotonic()
                        frozen = poses
                    assert poses == frozen, "Server actors drifted after match result"
                    frames += 1
                    if not replay_requested and time.monotonic() - ended_at >= 25:
                        assert frames >= 150
                        # Input button bit 4 is replay, on the same epoch.
                        sequence += 1
                        await ws.send(struct.pack("<BIIfffB", 4, epoch, sequence, 0, 0, 0, 4))
                        replay_requested = True
        raise AssertionError("Result/replay soak timed out")
    finally:
        sender.cancel()
        await asyncio.gather(sender, return_exceptions=True)


async def main(base):
    creator, code = await join(base, "/create")
    friend, joined = await join(base, "/join/" + code)
    assert code == joined
    try:
        result = await asyncio.gather(watch(creator), watch(friend))
        assert result[0][1:] == result[1][1:]
        print("ROOM_RESULT_SOAK_PASS endFrames=%s maps=%s holdSeconds=25" %
              ((result[0][0], result[1][0]), result[0][1:]))
    finally:
        await asyncio.gather(creator.close(), friend.close(), return_exceptions=True)


if __name__ == "__main__":
    asyncio.run(main(sys.argv[1]))
