import { api, post } from "./api";
type Fields = {
  placeId?: string;
  languageCode?: string;
  pageKey?: string;
  playId?: string;
  durationSeconds?: number;
  playedSeconds?: number;
  trigger?: string;
};
type Event = Fields & {
  eventId: string;
  type: string;
  clientTimestamp: string;
  sessionId: string;
};
let enabled = false,
  sessionId: string | null = null,
  last = 0,
  pending: Promise<void> | null = null;
let queue: { event: Event; created: number; attempts: number }[] = [];
export function consentChoice(): boolean | null {
  try {
    const v = JSON.parse(localStorage.getItem("sg-consent") ?? "null");
    return v && Date.now() - v.at < 90 * 86400000 ? !!v.enabled : null;
  } catch {
    return null;
  }
}
async function session() {
  if (!enabled) return;
  if (sessionId && Date.now() - last < 29 * 60000) return;
  if (!pending)
    pending = (async () => {
      const r = await post<{ sessionId: string }>("/analytics/session", {
        consent: true,
      });
      sessionId = r.sessionId;
      last = Date.now();
    })().finally(() => (pending = null));
  await pending;
}
export async function setAnalytics(value: boolean) {
  const previous = enabled;
  enabled = value;
  localStorage.setItem(
    "sg-consent",
    JSON.stringify({ enabled: value, at: Date.now() }),
  );
  if (value) {
    try {
      await session();
      if (!previous) {
        const path = window.location.pathname;
        if (path === "/" || path.startsWith("/places/") || path === "/privacy")
          await track("page_view", {
            pageKey: path.startsWith("/places/")
              ? "place_detail"
              : path === "/privacy"
                ? "privacy"
                : "explore",
          });
      }
    } catch {
      /* Product works even if analytics is unavailable */
    }
  } else {
    queue = [];
    if (pending)
      try {
        await pending;
      } catch {
        /* Wait for an in-flight handshake before deleting its cookie. */
      }
    sessionId = null;
    try {
      await api("/analytics/session", { method: "DELETE" });
    } catch {
      /* Collection stops locally even while offline. */
    }
  }
  window.dispatchEvent(
    new CustomEvent("sg:consent", {
      detail: { enabled: value, changed: previous !== value },
    }),
  );
}
export async function initAnalytics() {
  enabled = consentChoice() === true;
  if (enabled)
    try {
      await session();
    } catch {
      /* Optional telemetry */
    }
}
async function send(event: Event) {
  const r = await post<{ results: { status: string }[] }>("/analytics/events", {
    events: [event],
  });
  const ok = ["accepted", "duplicate"].includes(r.results[0]?.status);
  if (ok) last = Date.now();
  return ok;
}
export async function track(
  type: string,
  fields: Fields = {},
): Promise<boolean> {
  if (!enabled) return false;
  try {
    await session();
    if (!sessionId || !enabled) return false;
    const event: Event = {
      eventId: crypto.randomUUID(),
      type,
      clientTimestamp: new Date().toISOString(),
      sessionId,
      ...fields,
    };
    try {
      return await send(event);
    } catch {
      if (queue.length < 100)
        queue.push({ event, created: Date.now(), attempts: 0 });
      return false;
    }
  } catch {
    return false;
  }
}
async function flush() {
  if (!enabled) return;
  const now = Date.now();
  queue = queue.filter(
    (x) =>
      now - x.created < 300000 &&
      x.event.sessionId === sessionId &&
      x.attempts < 3,
  );
  for (const item of [...queue]) {
    item.attempts++;
    try {
      await send(item.event);
      queue = queue.filter((x) => x !== item);
    } catch {
      break;
    }
  }
}
if (typeof window !== "undefined") {
  window.addEventListener("online", () => void flush());
  setInterval(() => void flush(), 30000);
}
