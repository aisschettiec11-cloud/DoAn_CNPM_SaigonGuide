import { describe, it, expect } from "vitest";
import { Geofence, haversine, validSample } from "../src/lib/geofence";
import type { Sample } from "../src/lib/geofence";
const base = 1_000_000;
const p = { id: "a", latitude: 10.77, longitude: 106.69, triggerRadiusM: 75 };
const sample = (t: number, latitude = p.latitude, accuracy = 5): Sample => ({
  latitude,
  longitude: p.longitude,
  accuracy,
  timestamp: base + t,
});
describe("GPS and geofence business rules", () => {
  it("calculates Haversine independently of language or GPS permission", () => {
    expect(haversine(p, p)).toBe(0);
    expect(
      haversine(
        { latitude: 0, longitude: 0 },
        { latitude: 0, longitude: 0.001 },
      ),
    ).toBeCloseTo(111.1949266, 4);
  });
  it("rejects stale, imprecise, invalid and future samples", () => {
    expect(validSample(sample(0), base + 16000)).toBe(false);
    expect(validSample(sample(0, p.latitude, 41), base)).toBe(false);
    expect(validSample(sample(6000), base)).toBe(false);
    expect(validSample(sample(0, 91), base)).toBe(false);
  });
  it("requires two valid fixes and does not repeat inside", () => {
    const g = new Geofence();
    expect(g.update(sample(0), [p], base)).toBeNull();
    expect(g.update(sample(2000), [p], base + 2000)).toBeNull();
    expect(g.update(sample(3000), [p], base + 3000)?.id).toBe("a");
    for (let t = 6000; t <= 60000; t += 3000)
      expect(g.update(sample(t), [p], base + t)).toBeNull();
  });
  it("resets candidates for stale fixes or intervals beyond 10 seconds", () => {
    const g = new Geofence();
    g.update(sample(0), [p], base);
    expect(g.update(sample(11000), [p], base + 11000)).toBeNull();
    expect(g.update(sample(14000), [p], base + 14000)?.id).toBe("a");
  });
  it("persists latch without storing coordinates", () => {
    const g = new Geofence();
    g.update(sample(0), [p], base);
    g.update(sample(3000), [p], base + 3000);
    expect(g.snapshot()).not.toContain("latitude");
    const next = new Geofence(g.snapshot());
    expect(next.update(sample(6000), [p], base + 6000)).toBeNull();
  });
  it("requires exit hysteresis and 120 seconds before rearm", () => {
    const g = new Geofence();
    g.update(sample(0), [p], base);
    g.update(sample(3000), [p], base + 3000);
    g.update(sample(6000, p.latitude + 0.002), [p], base + 6000);
    g.update(sample(9000, p.latitude + 0.002), [p], base + 9000);
    expect(g.update(sample(119000), [p], base + 119000)).toBeNull();
    expect(g.update(sample(123000), [p], base + 123000)).toBeNull();
    expect(g.update(sample(126000), [p], base + 126000)?.id).toBe("a");
  });
  it("selects one candidate deterministically and never interrupts a busy player", () => {
    const g = new Geofence();
    g.update(sample(0), [{ ...p, id: "b" }, p], base);
    expect(
      g.update(sample(3000), [{ ...p, id: "b" }, p], base + 3000, true),
    ).toBeNull();
    expect(
      g.update(sample(6000), [{ ...p, id: "b" }, p], base + 6000)?.id,
    ).toBe("a");
  });
});
