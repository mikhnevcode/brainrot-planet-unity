"""Loopback-only public match gateway with bounded, isolated Unity rooms.

TLS terminates in the dedicated tunnel. No URL or shared server key is entered
by players. Each room owns one authoritative Unity process and at most four
WebSocket peers; all traffic after RoomInfo remains the existing binary match
protocol. This is a guest-play alpha, not an account or payment service.
"""
import asyncio
import contextlib
from dataclasses import dataclass, field
from http import HTTPStatus
import logging
import os
from pathlib import Path
import secrets
import struct
import time

from websockets.asyncio.server import serve


ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
MAX_PACKET = 196608
MAX_CLIENT_PACKET = 2048
ROOM_INFO = 7  # gateway -> client: BinaryWriter string, no Unity-server change


@dataclass
class Room:
    code: str
    private: bool
    slot: int
    port: int = 0
    process: object = None
    ready: asyncio.Task = None
    activated: asyncio.Event = field(default_factory=asyncio.Event)
    clients: int = 0
    started: bool = False
    closed: bool = False
    idle_task: asyncio.Task = None


class RoomGateway:
    def __init__(self, server_exe=None, first_port=28100, max_rooms=8,
                 runtime_dir=None, idle_seconds=20):
        if not 1 <= max_rooms <= 32 or first_port < 1024 or first_port+max_rooms > 65535:
            raise ValueError("Invalid bounded room port range")
        self.server_exe = Path(server_exe).resolve() if server_exe else None
        self.first_port = first_port
        self.max_rooms = max_rooms
        self.runtime_dir = Path(runtime_dir or "runtime/rooms").resolve()
        self.idle_seconds = idle_seconds
        self.rooms = {}
        self.lock = asyncio.Lock()
        self.stopping = False

    @staticmethod
    def route(path):
        if path == "/match/quick":
            return "quick", ""
        if path == "/match/create":
            return "create", ""
        if path.startswith("/match/join/"):
            code = path[len("/match/join/"):].upper()
            if len(code) == 6 and all(letter in ALPHABET for letter in code):
                return "join", code
        return None, ""

    def authorize(self, connection, request):
        mode, code = self.route(request.path)
        if mode is None:
            return connection.respond(HTTPStatus.NOT_FOUND, "Unknown match route\n")
        if mode == "join":
            room = self.rooms.get(code)
            if room is None or room.closed or room.started or room.clients >= 4:
                return connection.respond(HTTPStatus.NOT_FOUND, "Room unavailable\n")
        elif mode == "create" and len(self.rooms) >= self.max_rooms:
            return connection.respond(HTTPStatus.SERVICE_UNAVAILABLE, "Rooms busy\n")
        elif mode == "quick" and len(self.rooms) >= self.max_rooms and not any(
                not room.private and not room.started and not room.closed and room.clients < 4
                for room in self.rooms.values()):
            return connection.respond(HTTPStatus.SERVICE_UNAVAILABLE, "Rooms busy\n")

    def new_code(self):
        while True:
            code = "".join(secrets.choice(ALPHABET) for _ in range(6))
            if code not in self.rooms:
                return code

    async def reserve(self, mode, code):
        async with self.lock:
            if self.stopping:
                return None
            room = None
            if mode == "join":
                room = self.rooms.get(code)
                if room is None or room.closed or room.started or room.clients >= 4:
                    return None
            elif mode == "quick":
                room = next((candidate for candidate in self.rooms.values()
                             if not candidate.private and not candidate.closed and
                             not candidate.started and candidate.clients < 4), None)
            if room is None:
                if mode == "join" or len(self.rooms) >= self.max_rooms:
                    return None
                used = {candidate.slot for candidate in self.rooms.values()}
                slot = next(index for index in range(self.max_rooms) if index not in used)
                room = Room(self.new_code(), mode == "create", slot)
                self.rooms[room.code] = room
            if room.idle_task is not None:
                room.idle_task.cancel()
                room.idle_task = None
            room.clients += 1
            if not room.private or room.clients >= 2:
                if room.ready is None:
                    room.ready = asyncio.create_task(self.start_backend(room))
                room.activated.set()
            return room

    async def start_backend(self, room):
        """Override in unit tests to supply a fake framed TCP backend."""
        if self.server_exe is None or not self.server_exe.is_file():
            raise FileNotFoundError("Unity room server executable missing")
        self.runtime_dir.mkdir(parents=True, exist_ok=True)
        room.port = self.first_port + room.slot
        environment = os.environ.copy()
        environment["BRAINROT_MATCH_PORT"] = str(room.port)
        log = self.runtime_dir / ("room-" + room.code + ".log")
        room.process = await asyncio.create_subprocess_exec(
            str(self.server_exe), "-batchmode", "-nographics", "-brainrot-server",
            "-logFile", str(log), cwd=str(self.server_exe.parent), env=environment,
            stdin=asyncio.subprocess.DEVNULL, stdout=asyncio.subprocess.DEVNULL,
            stderr=asyncio.subprocess.DEVNULL)
        deadline = time.monotonic() + 25
        while time.monotonic() < deadline:
            if room.process.returncode is not None:
                raise RuntimeError("Unity room server exited at startup")
            if log.exists() and "MATCH_SERVER_READY loopbackPort=" + str(room.port) in log.read_text(errors="ignore"):
                logging.info("ROOM_READY code=%s private=%s port=%d", room.code,
                             room.private, room.port)
                return
            await asyncio.sleep(.25)
        raise TimeoutError("Unity room server startup timeout")

    async def handle(self, websocket):
        mode, code = self.route(websocket.request.path)
        if mode is None:
            await websocket.close(1008, "Invalid match route")
            return
        room = await self.reserve(mode, code)
        if room is None:
            await websocket.close(1013, "Room unavailable")
            return
        writer = None
        tasks = []
        try:
            # The private code is available immediately, even while the Unity
            # worker boots. Quick-match rooms keep their internal code hidden.
            visible_code = room.code if room.private else ""
            info = bytes((ROOM_INFO, len(visible_code))) + visible_code.encode("ascii")
            await websocket.send(info)
            # A private host can take time to share the code. Keep the client
            # informed, but do not open Unity's ten-second lobby until a friend
            # actually joins. Quick rooms start immediately, including bots.
            while not room.activated.is_set():
                joined = asyncio.create_task(room.activated.wait())
                left = asyncio.create_task(websocket.wait_closed())
                try:
                    done, _ = await asyncio.wait((joined, left), timeout=8,
                                                 return_when=asyncio.FIRST_COMPLETED)
                finally:
                    for task in (joined, left):
                        if not task.done():
                            task.cancel()
                    await asyncio.gather(joined, left, return_exceptions=True)
                if left in done:
                    return
                if not done:
                    await websocket.send(info)
            await asyncio.wait_for(room.ready, 27)
            reader, writer = await asyncio.wait_for(
                asyncio.open_connection("127.0.0.1", room.port), 3)

            async def incoming():
                tokens, previous = 50.0, time.monotonic()
                async for data in websocket:
                    now = time.monotonic()
                    tokens = min(50.0, tokens + (now - previous) * 30) - 1
                    previous = now
                    if tokens < 0 or not isinstance(data, bytes) or not 1 <= len(data) <= MAX_CLIENT_PACKET:
                        await websocket.close(1008, "Invalid client traffic")
                        return
                    writer.write(struct.pack("<I", len(data)) + data)
                    await asyncio.wait_for(writer.drain(), 5)

            async def outgoing():
                while True:
                    header = await asyncio.wait_for(reader.readexactly(4), 15)
                    size, = struct.unpack("<I", header)
                    if not 1 <= size <= MAX_PACKET:
                        raise ValueError("Invalid Unity frame")
                    data = await asyncio.wait_for(reader.readexactly(size), 5)
                    if data[0] == 3:  # MatchProtocol.Start: late admission closes.
                        room.started = True
                    await asyncio.wait_for(websocket.send(data), 5)

            tasks = [asyncio.create_task(incoming()), asyncio.create_task(outgoing())]
            done, _ = await asyncio.wait(tasks, return_when=asyncio.FIRST_COMPLETED)
            for task in done:
                task.result()
        except Exception as error:
            # Never log client frames or request headers.
            logging.info("ROOM_PEER_ENDED code=%s reason=%s", room.code,
                         type(error).__name__)
        finally:
            for task in tasks:
                task.cancel()
            await asyncio.gather(*tasks, return_exceptions=True)
            if writer is not None:
                writer.close()
                with contextlib.suppress(Exception):
                    await writer.wait_closed()
            async with self.lock:
                room.clients -= 1
                if room.clients == 0 and not room.closed and not self.stopping:
                    room.idle_task = asyncio.create_task(self.reap_after_idle(room))
            with contextlib.suppress(Exception):
                await websocket.close()

    async def reap_after_idle(self, room):
        try:
            await asyncio.sleep(self.idle_seconds)
            async with self.lock:
                if room.clients != 0 or room.closed:
                    return
                room.closed = True
                self.rooms.pop(room.code, None)
            await self.stop_backend(room)
            logging.info("ROOM_REAPED code=%s", room.code)
        except asyncio.CancelledError:
            return

    async def stop_backend(self, room):
        if room.ready is not None and not room.ready.done():
            room.ready.cancel()
            await asyncio.gather(room.ready, return_exceptions=True)
        process = room.process
        if process is not None and process.returncode is None:
            process.terminate()
            try:
                await asyncio.wait_for(process.wait(), 5)
            except asyncio.TimeoutError:
                process.kill()
                await process.wait()

    async def close(self):
        self.stopping = True
        async with self.lock:
            rooms = list(self.rooms.values())
            self.rooms.clear()
            for room in rooms:
                room.closed = True
                if room.idle_task is not None:
                    room.idle_task.cancel()
        await asyncio.gather(*(self.stop_backend(room) for room in rooms),
                             return_exceptions=True)


async def main():
    executable = os.environ["BRAINROT_ROOM_SERVER_EXE"]
    gateway = RoomGateway(executable,
                          first_port=int(os.environ.get("BRAINROT_ROOM_BASE_PORT", "28100")),
                          max_rooms=int(os.environ.get("BRAINROT_MAX_ROOMS", "8")),
                          runtime_dir=os.environ.get("BRAINROT_ROOM_RUNTIME", "runtime/rooms"))
    port = int(os.environ.get("BRAINROT_GATEWAY_PORT", "28180"))
    try:
        async with serve(gateway.handle, "127.0.0.1", port,
                         process_request=gateway.authorize, origins=[None],
                         compression=None, max_size=MAX_CLIENT_PACKET, max_queue=8,
                         open_timeout=5, ping_interval=5, ping_timeout=10,
                         close_timeout=2, server_header=None) as server:
            logging.info("ROOM_GATEWAY_READY loopbackPort=%d maxRooms=%d", port,
                         gateway.max_rooms)
            await server.serve_forever()
    finally:
        await gateway.close()


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(message)s")
    asyncio.run(main())
