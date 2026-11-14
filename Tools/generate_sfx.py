"""Генератор коротких звуковых эффектов игры без внешних библиотек.

Пишет моно-WAV (22 050 Гц, 16 бит) в Game/assets/audio/sfx.
Генератор детерминирован: повторный запуск даёт те же файлы.
"""

import math
import random
import wave
from array import array
from pathlib import Path

SAMPLE_RATE = 22050
TAU = 2.0 * math.pi
OUTPUT = Path(__file__).resolve().parent.parent / "Game" / "assets" / "audio" / "sfx"


def note(midi):
    """Частота ноты по номеру MIDI (69 — ля первой октавы, 440 Гц)."""
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def silence(duration):
    return array("f", [0.0]) * int(duration * SAMPLE_RATE)


def mallet(freq, duration, decay=6.0, gain=1.0):
    """Мягкая маримба: основной тон и две гармоники с быстрым затуханием."""
    result = array("f")
    for i in range(int(duration * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        envelope = min(1.0, t / 0.006) * math.exp(-decay * t)
        phase = TAU * freq * t
        value = math.sin(phase)
        value += 0.22 * math.sin(2 * phase) * math.exp(-decay * 2.2 * t)
        value += 0.08 * math.sin(4 * phase) * math.exp(-decay * 3.5 * t)
        result.append(value * envelope * gain)
    return result


def drop(freq_from, freq_to, duration, gain=1.0):
    """«Капля»: тон со скольжением вниз и экспоненциальным затуханием."""
    result = array("f")
    count = int(duration * SAMPLE_RATE)
    phase = 0.0
    for i in range(count):
        t = i / SAMPLE_RATE
        k = t / duration
        phase += TAU * (freq_from + (freq_to - freq_from) * k) / SAMPLE_RATE
        envelope = min(1.0, t / 0.004) * math.exp(-7.0 * k)
        result.append(math.sin(phase) * envelope * gain)
    return result


def thud(freq, duration, gain=1.0, seed=1):
    """Глухой низкий удар без резких обертонов: низкий тон и шумовой слой."""
    rng = random.Random(seed)
    result = array("f")
    count = int(duration * SAMPLE_RATE)
    filtered = 0.0
    for i in range(count):
        t = i / SAMPLE_RATE
        envelope = math.exp(-9.0 * (t / duration))
        filtered += 0.12 * (rng.uniform(-1.0, 1.0) - filtered)
        result.append((math.sin(TAU * freq * t) * 0.9 + filtered * 0.35) * envelope * gain)
    return result


def tick(duration, cutoff, gain=1.0, seed=2):
    """Короткий мягкий щелчок: отфильтрованный шум."""
    rng = random.Random(seed)
    result = array("f")
    count = int(duration * SAMPLE_RATE)
    filtered = 0.0
    for i in range(count):
        t = i / SAMPLE_RATE
        envelope = math.sin(math.pi * (t / duration)) ** 1.6
        filtered += cutoff * (rng.uniform(-1.0, 1.0) - filtered)
        result.append(filtered * envelope * gain)
    return result


def buzz(duration, freq=190.0, rate=110.0, gain=0.75):
    """Жужжание: тон с быстрой амплитудной модуляцией и быстрым спадом."""
    result = array("f")
    count = int(duration * SAMPLE_RATE)
    for i in range(count):
        t = i / SAMPLE_RATE
        envelope = min(1.0, t / 0.005) * math.exp(-6.0 * (t / duration))
        wobble = 0.35 + 0.65 * (0.5 + 0.5 * math.sin(TAU * rate * t))
        carrier = math.sin(TAU * freq * t) + 0.3 * math.sin(TAU * freq * 2.0 * t)
        result.append(carrier * wobble * envelope * gain)
    return result


def mix(*parts):
    """Складывает несколько фрагментов, выравнивая их по началу."""
    length = max(len(part) for part in parts)
    out = array("f", [0.0]) * length
    for part in parts:
        for i, value in enumerate(part):
            out[i] += value
    return out


def sequence(notes, decay=6.0):
    """Раскладывает ноты во времени: (частота, смещение, длительность)."""
    length = max(offset + duration for _, offset, duration in notes)
    out = silence(length)
    for freq, offset, duration in notes:
        start = int(offset * SAMPLE_RATE)
        part = mallet(freq, duration, decay)
        for i, value in enumerate(part):
            out[start + i] += value
    return out


def write(name, data, peak=0.9):
    """Нормализует по пику и записывает моно-WAV."""
    maximum = max(abs(value) for value in data)
    scale = peak / maximum if maximum > 0 else 1.0
    pcm = array("h")
    for value in data:
        sample = max(-1.0, min(1.0, value * scale))
        pcm.append(round(sample * 32767))
    OUTPUT.mkdir(parents=True, exist_ok=True)
    target = OUTPUT / f"{name}.wav"
    with wave.open(str(target), "wb") as file:
        file.setnchannels(1)
        file.setsampwidth(2)
        file.setframerate(SAMPLE_RATE)
        file.writeframes(pcm.tobytes())
    rms = math.sqrt(sum(value * value for value in data) / len(data)) * scale
    print(f"{target.name}: {len(data) / SAMPLE_RATE:.3f} s, peak={peak:.2f}, RMS={rms:.3f}")


def build():
    # Поимка фрукта: мягкая «капля» с лёгким щелчком воды.
    write("catch", mix(drop(1080, 640, 0.13), tick(0.02, 0.5, 0.22)))

    # Золотой фрукт: короткая восходящая искра из трёх нот.
    write("golden", mix(
        sequence([(note(79), 0.0, 0.14), (note(83), 0.07, 0.14), (note(88), 0.14, 0.22)], decay=7.0),
        tick(0.06, 0.45, 0.18, seed=11)))

    # Хилка: тёплые две ноты вверх.
    write("heal", sequence([(note(72), 0.0, 0.2), (note(76), 0.1, 0.26)], decay=5.0))

    # Урон: тихий глухой удар, без резких частот.
    write("damage", thud(150.0, 0.2))

    # Отскок мусора: короткий «тик».
    write("deflect", mix(tick(0.05, 0.6, 0.8), mallet(note(86), 0.06, 14.0, 0.4)))

    # Новый уровень: восходящие три ноты.
    write("level_up", sequence([(note(69), 0.0, 0.2), (note(73), 0.09, 0.2), (note(76), 0.18, 0.26)], decay=5.0))

    # Открылся выбор перка: мягкий двухнотный чайм.
    write("perk_offer", sequence([(note(74), 0.0, 0.22), (note(81), 0.12, 0.28)], decay=4.5))

    # Перк выбран: короткий подтверждающий «тик».
    write("perk_pick", sequence([(note(84), 0.0, 0.09), (note(88), 0.05, 0.12)], decay=10.0))

    # Рост комбо: яркая искра вверх.
    write("combo", sequence([(note(81), 0.0, 0.1), (note(86), 0.05, 0.14)], decay=8.0))

    # Конец игры: мягкая нисходящая фраза.
    write("game_over", sequence([(note(76), 0.0, 0.24), (note(72), 0.16, 0.24), (note(67), 0.32, 0.42)], decay=4.0))

    # Suika: бросок фрукта — «плюх».
    write("suika_throw", drop(780, 520, 0.11))

    # Suika: слияние — одна нота; питч-лестницу задаёт плеер по уровню фрукта.
    write("suika_merge", mallet(note(76), 0.18, 5.0))

    # Кнопки интерфейса: очень тихий щелчок.
    write("ui_tap", tick(0.045, 0.8, 0.5, seed=7))

    # Поимка мухи: жужжание, обрывающееся коротким шлепком.
    write("fly_catch", mix(buzz(0.12), tick(0.035, 0.7, 0.6, seed=13)))


if __name__ == "__main__":
    build()
