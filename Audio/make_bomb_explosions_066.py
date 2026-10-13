"""Original impact/rumble explosion sounds; no borrowed samples.
Run with Python 3 (standard library only). Writes three Unity-ready PCM WAVs.
"""
import json, math, random, struct, wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Audio'
RATE = 24000
DURATION = 1.8

def explosion(seed):
    rng = random.Random(seed)
    samples = []
    low = body = high = 0.0
    phases = [rng.uniform(0, math.tau) for _ in range(3)]
    debris = [(rng.uniform(.07, .48), rng.uniform(.035, .08)) for _ in range(11)]
    for i in range(round(RATE * DURATION)):
        t = i / RATE
        noise = rng.uniform(-1, 1)
        low += .032 * (noise - low)
        body += .18 * (noise - body)
        high += .43 * (noise - high)
        # Broadband pressure crack, not a rising/falling laser oscillator.
        crack = (noise - high) * math.exp(-t * 52) * .66
        # Phone-audible low mids plus bass; several inharmonic fixed resonances.
        thump = sum(math.sin(math.tau * f * t + p) * a
                    for f, p, a in zip((67, 113, 173), phases, (.35, .22, .12)))
        thump *= math.exp(-t * 9)
        roar = body * 2.4 * math.exp(-t * 5.2)
        rumble = low * 2.3 * (1 - math.exp(-t * 35)) * math.exp(-t * 2.8)
        grit = sum(a * math.exp(-(t - start) * 65) for start, a in debris if t >= start)
        reflection = 0.0
        for delay, gain in ((.085, .30), (.17, .17), (.29, .09)):
            if t > delay:
                reflection += body * gain * math.exp(-(t - delay) * 9)
        attack = min(1.0, t / .0015)
        fade = min(1.0, (DURATION - t) / .14)
        samples.append((crack + thump + roar + rumble + noise * grit + reflection) * attack * fade)
    # Remove DC before a final end taper; reserve mix headroom for a three-bomb volley.
    mean = sum(samples) / len(samples)
    samples = [(x - mean) * min(1, i / 36, (len(samples)-1-i) / 3360)
               for i, x in enumerate(samples)]
    peak = max(abs(x) for x in samples)
    return [x * .88 / peak for x in samples]

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    report = []
    for variant in range(3):
        data = explosion(6601 + variant * 271)
        target = OUT / ('BombExplosion' + str(variant + 1) + '.wav')
        with wave.open(str(target), 'wb') as f:
            f.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
            f.writeframes(struct.pack('<' + 'h' * len(data), *(round(x * 32767) for x in data)))
        early = data[:RATE // 4]
        tail = data[RATE:]
        metrics = dict(file=target.name, seconds=len(data)/RATE,
                       peak=max(abs(x) for x in data),
                       early_rms=math.sqrt(sum(x*x for x in early)/len(early)),
                       tail_rms=math.sqrt(sum(x*x for x in tail)/len(tail)))
        assert metrics['peak'] <= .881 and metrics['early_rms'] > .1
        assert metrics['tail_rms'] < metrics['early_rms'] * .12
        report.append(metrics)
    print(json.dumps(report, indent=2))

if __name__ == '__main__':
    main()
