"""Two independent internet peers; asserts identical authoritative snapshots.
Usage: BRAINROT_QA_KEY environment variable, WSS URL as argv[1].
Runs a real room for 30 seconds, then disconnects without altering other services.
"""
import asyncio
import hashlib
import os
import struct
import sys
import time
from websockets.asyncio.client import connect


async def main():
    histories = [{}, {}]
    teams = [0, 0]

    async def peer(index):
        async with connect(sys.argv[1], additional_headers={"X-Brainrot-Test-Key": os.environ["BRAINROT_QA_KEY"]},
                           max_size=196608, compression=None, proxy=None) as ws:
            await ws.send(struct.pack("<BiB", 1, 1, index))
            epoch, sequence = 0, 0
            began = time.monotonic()

            async def inputs():
                nonlocal sequence
                while True:
                    sequence += 1
                    # Spectator-like stationary participants keep this test reproducible.
                    await ws.send(struct.pack("<BIIfffB", 4, epoch, sequence, 0, 0, 0, 0))
                    await asyncio.sleep(.05)

            sender = asyncio.create_task(inputs())
            try:
                while time.monotonic()-began < 30:
                    packet = await asyncio.wait_for(ws.recv(), 15)
                    if packet[0] == 3:
                        epoch, teams[index] = struct.unpack_from("<IB", packet, 1)
                    elif packet[0] == 5:
                        received_epoch, = struct.unpack_from("<I", packet, 1)
                        assert received_epoch == epoch and epoch > 0
                        # Epoch + exact remaining-time bytes identify a shared snapshot.
                        key = packet[1:5] + packet[7:11]
                        histories[index][key] = hashlib.sha256(packet).hexdigest()
            finally:
                sender.cancel()
                await asyncio.gather(sender, return_exceptions=True)

    await asyncio.gather(peer(0), peer(1))
    common = histories[0].keys() & histories[1].keys()
    assert len(common) >= 100, f"Too few shared snapshots: {len(common)}"
    assert all(histories[0][key] == histories[1][key] for key in common)
    assert teams[0] != teams[1] and min(teams) >= 1
    print(f"WSS_TWO_PEERS_PASS teams={teams} identicalSnapshots={len(common)}")


if __name__ == "__main__":
    asyncio.run(main())
