export type Point = {
  id: string;
  latitude: number;
  longitude: number;
  triggerRadiusM: number;
};
export type Sample = {
  latitude: number;
  longitude: number;
  accuracy: number;
  timestamp: number;
};
type State = {
  latched: boolean;
  entryAt: number;
  exited: boolean;
  insideAt?: number;
  outsideAt?: number;
};
export const haversine = (
  a: { latitude: number; longitude: number },
  b: { latitude: number; longitude: number },
) => {
  const r = Math.PI / 180;
  const dlat = (b.latitude - a.latitude) * r,
    dlon = (b.longitude - a.longitude) * r;
  const x =
    Math.sin(dlat / 2) ** 2 +
    Math.cos(a.latitude * r) *
      Math.cos(b.latitude * r) *
      Math.sin(dlon / 2) ** 2;
  return 6371000 * 2 * Math.atan2(Math.sqrt(x), Math.sqrt(Math.max(0, 1 - x)));
};
export function validSample(s: Sample, now: number) {
  return (
    Number.isFinite(s.latitude) &&
    Number.isFinite(s.longitude) &&
    s.latitude >= -90 &&
    s.latitude <= 90 &&
    s.longitude >= -180 &&
    s.longitude <= 180 &&
    Number.isFinite(s.accuracy) &&
    s.accuracy >= 0 &&
    s.accuracy <= 40 &&
    s.timestamp >= now - 15000 &&
    s.timestamp <= now + 5000
  );
}
export class Geofence {
  states = new Map<string, State>();
  private last = 0;
  constructor(saved?: string | null) {
    if (saved)
      try {
        const parsed = JSON.parse(saved) as Record<string, State>;
        for (const [id, s] of Object.entries(parsed))
          if (typeof s.latched === "boolean" && typeof s.entryAt === "number")
            this.states.set(id, {
              latched: s.latched,
              entryAt: s.entryAt,
              exited: !!s.exited,
            });
      } catch {
        /* Ignore corrupt local preference */
      }
  }
  snapshot() {
    return JSON.stringify(
      Object.fromEntries(
        [...this.states].map(([k, s]) => [
          k,
          { latched: s.latched, entryAt: s.entryAt, exited: s.exited },
        ]),
      ),
    );
  }
  resetSamples() {
    this.last = 0;
    for (const s of this.states.values()) {
      delete s.insideAt;
      delete s.outsideAt;
    }
  }
  update(
    sample: Sample,
    places: Point[],
    now = Date.now(),
    busy = false,
  ): Point | null {
    if (!validSample(sample, now)) {
      this.resetSamples();
      return null;
    }
    if (sample.timestamp <= this.last || sample.timestamp - this.last < 3000)
      return null;
    this.last = sample.timestamp;
    const eligible: { p: Point; d: number }[] = [];
    for (const p of places) {
      const s = this.states.get(p.id) ?? {
        latched: false,
        entryAt: 0,
        exited: false,
      };
      this.states.set(p.id, s);
      const d = haversine(sample, p);
      if (s.latched) {
        if (d - sample.accuracy >= p.triggerRadiusM + 30) {
          if (
            s.outsideAt !== undefined &&
            sample.timestamp - s.outsideAt >= 3000 &&
            sample.timestamp - s.outsideAt <= 10000
          )
            s.exited = true;
          else s.outsideAt = sample.timestamp;
        } else delete s.outsideAt;
        if (s.exited && now - s.entryAt >= 120000) {
          s.latched = false;
          s.exited = false;
          delete s.insideAt;
        } else continue;
      }
      if (d + sample.accuracy <= p.triggerRadiusM) {
        if (
          s.insideAt !== undefined &&
          sample.timestamp - s.insideAt >= 3000 &&
          sample.timestamp - s.insideAt <= 10000
        ) {
          if (!busy) eligible.push({ p, d });
        } else s.insideAt = sample.timestamp;
      } else delete s.insideAt;
    }
    eligible.sort((a, b) => a.d - b.d || a.p.id.localeCompare(b.p.id));
    const selected = eligible[0]?.p;
    if (selected) {
      const s = this.states.get(selected.id)!;
      s.latched = true;
      s.entryAt = now;
      s.exited = false;
      delete s.insideAt;
      return selected;
    }
    return null;
  }
}
