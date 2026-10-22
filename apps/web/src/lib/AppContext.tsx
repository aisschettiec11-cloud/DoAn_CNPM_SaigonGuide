import { createContext, useContext, useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import { useLocation } from "react-router-dom";
import { api, refreshCsrf } from "./api";
import type { Locale, Place, User } from "./types";
import { Geofence, validSample } from "./geofence";
import type { Sample } from "./geofence";
import { initAnalytics, track } from "./analytics";
type AudioState =
  | "idle"
  | "loading"
  | "playing"
  | "paused"
  | "stopped"
  | "blocked"
  | "error"
  | "ended";
type AppState = {
  locale: Locale;
  setLocale: (l: Locale) => void;
  user: User | null;
  authReady: boolean;
  reloadUser: () => Promise<void>;
  places: Place[];
  placesLoading: boolean;
  placesError: string;
  reloadPlaces: () => void;
  play: (
    p: Place,
    trigger?: "manual" | "auto",
    replay?: boolean,
  ) => Promise<void>;
  pause: () => void;
  resume: () => void;
  stop: () => void;
  current: Place | null;
  audioState: AudioState;
  audioError: string;
  elapsed: number;
  duration: number;
  gps: boolean;
  setGps: (on: boolean) => void;
  gpsStatus: string;
  sample: Sample | null;
  auto: boolean;
  setAuto: (on: boolean) => void;
  nearby: Place | null;
  dismissNearby: () => void;
};
const Context = createContext<AppState | null>(null);
export const useApp = () => {
  const c = useContext(Context);
  if (!c) throw new Error("Missing AppProvider");
  return c;
};
export function AppProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(() =>
    localStorage.getItem("sg-locale") === "en" ? "en" : "vi",
  );
  const [user, setUser] = useState<User | null>(null);
  const [authReady, setAuthReady] = useState(false);
  const [places, setPlaces] = useState<Place[]>([]);
  const [placesLoading, setPlacesLoading] = useState(true);
  const [placesError, setPlacesError] = useState("");
  const [revision, setRevision] = useState(0);
  const [current, setCurrent] = useState<Place | null>(null),
    [audioState, setAudioState] = useState<AudioState>("idle"),
    [audioError, setAudioError] = useState(""),
    [elapsed, setElapsed] = useState(0),
    [duration, setDuration] = useState(0);
  const audio = useRef<HTMLAudioElement | null>(null);
  const playback = useRef<{
    p: Place;
    id: string;
    trigger: "manual" | "auto";
    started: boolean;
    ack: Promise<boolean> | null;
  } | null>(null);
  const [gps, setGpsState] = useState(false),
    [gpsStatus, setGpsStatus] = useState("off"),
    [sample, setSample] = useState<Sample | null>(null),
    [auto, setAuto] = useState(false),
    [nearby, setNearby] = useState<Place | null>(null);
  const tracking = useRef(false);
  const watcher = useRef<number | null>(null);
  const geo = useRef<Geofence>(
    new Geofence(sessionStorage.getItem("sg-geofence")),
  );
  const live = useRef({ places, auto, busy: false, locale });
  live.current = {
    places,
    auto,
    busy:
      audioState === "playing" || audioState === "loading" || nearby !== null,
    locale,
  };
  const location = useLocation();
  async function reloadUser() {
    try {
      setUser(await api<User>("/auth/me"));
    } catch {
      setUser(null);
    } finally {
      setAuthReady(true);
    }
  }
  useEffect(() => {
    void reloadUser();
    void initAnalytics();
  }, []);
  useEffect(() => {
    let cancelled = false;
    setPlacesLoading(true);
    setPlacesError("");
    api<Place[]>(`/places?language=${locale}`)
      .then((p) => {
        if (!cancelled) setPlaces(p);
      })
      .catch((e) => {
        if (!cancelled) setPlacesError(e.message);
      })
      .finally(() => {
        if (!cancelled) setPlacesLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [locale, revision]);
  useEffect(() => {
    if (!(
      location.pathname === "/" ||
      location.pathname.startsWith("/places/") ||
      location.pathname === "/privacy"
    ))
      return;
    void track("page_view", {
      pageKey: location.pathname.startsWith("/places/")
        ? "place_detail"
        : location.pathname === "/privacy"
          ? "privacy"
          : "explore",
      languageCode: locale,
    });
  }, [location.key]);
  useEffect(() => {
    const a = new Audio();
    audio.current = a;
    const playing = () => {
      setAudioState("playing");
      const p = playback.current;
      if (p && !p.started) {
        p.started = true;
        p.ack = track("narration_started", {
          placeId: p.p.id,
          languageCode: p.p.languageCode,
          playId: p.id,
          trigger: p.trigger,
        });
      }
    };
    const update = () => {
      setElapsed(a.currentTime);
      if (Number.isFinite(a.duration)) setDuration(a.duration);
    };
    const ended = async () => {
      setAudioState("ended");
      const p = playback.current;
      if (!p || !p.ack) return;
      let seconds = 0;
      for (let i = 0; i < a.played.length; i++)
        seconds += a.played.end(i) - a.played.start(i);
      if (!(await p.ack)) return;
      await track("narration_completed", {
        placeId: p.p.id,
        languageCode: p.p.languageCode,
        playId: p.id,
        playedSeconds:
          Math.round(
            Math.min(seconds, p.p.durationSeconds ?? a.duration) * 1000,
          ) / 1000,
      });
    };
    const error = () => {
      setAudioState("error");
      setAudioError(
        "Âm thanh hiện không tải được. Bạn có thể đọc toàn bộ bài bên dưới.",
      );
    };
    a.addEventListener("playing", playing);
    a.addEventListener("timeupdate", update);
    a.addEventListener("loadedmetadata", update);
    a.addEventListener("ended", ended);
    a.addEventListener("error", error);
    return () => {
      a.pause();
      a.removeAttribute("src");
      a.removeEventListener("playing", playing);
      a.removeEventListener("timeupdate", update);
      a.removeEventListener("loadedmetadata", update);
      a.removeEventListener("ended", ended);
      a.removeEventListener("error", error);
    };
  }, []);
  function stop() {
    const a = audio.current;
    if (a) {
      a.pause();
      a.currentTime = 0;
    }
    playback.current = null;
    setAudioState("stopped");
    setElapsed(0);
  }
  function setLocale(l: Locale) {
    if (l === locale) return;
    stop();
    setCurrent(null);
    setLocaleState(l);
    localStorage.setItem("sg-locale", l);
    void track("language_selected", { languageCode: l });
    document.documentElement.lang = l;
  }
  async function play(
    p: Place,
    trigger: "manual" | "auto" = "manual",
    replay = false,
  ) {
    if (!p.audioAssetId) {
      setAudioError(
        locale === "vi"
          ? "Chưa có âm thanh. Vui lòng đọc nội dung."
          : "Audio is unavailable. Please read the transcript.",
      );
      return;
    }
    const a = audio.current;
    if (!a) return;
    if (
      current &&
      current.id !== p.id &&
      audioState === "playing" &&
      trigger === "manual" &&
      !window.confirm(
        locale === "vi"
          ? "Dừng bài hiện tại và nghe địa điểm này?"
          : "Stop the current narration and switch?",
      )
    )
      return;
    if (
      !replay &&
      playback.current?.p.id === p.id &&
      playback.current.p.languageCode === p.languageCode &&
      audioState === "paused"
    ) {
      await resume();
      return;
    }
    a.pause();
    setCurrent(p);
    setElapsed(0);
    setDuration(p.durationSeconds ?? 0);
    setAudioState("loading");
    setAudioError("");
    playback.current = {
      p,
      id: crypto.randomUUID(),
      trigger,
      started: false,
      ack: null,
    };
    a.src = `/api/v1/media/${p.audioAssetId}`;
    try {
      await a.play();
    } catch (e) {
      setAudioState("blocked");
      setAudioError(
        e instanceof DOMException && e.name === "NotAllowedError"
          ? locale === "vi"
            ? "Trình duyệt cần bạn bấm Nghe để bắt đầu."
            : "Tap Listen to allow audio playback."
          : locale === "vi"
            ? "Không thể phát âm thanh. Bạn vẫn có thể đọc nội dung."
            : "Audio unavailable. The transcript is still available.",
      );
    }
  }
  async function resume() {
    try {
      await audio.current?.play();
    } catch {
      setAudioState("blocked");
    }
  }
  function pause() {
    audio.current?.pause();
    setAudioState("paused");
  }
  function setGps(on: boolean) {
    tracking.current = on;
    setGpsState(on);
    if (!on) {
      if (watcher.current !== null) {
        navigator.geolocation?.clearWatch(watcher.current);
        watcher.current = null;
      }
      setSample(null);
      setGpsStatus("off");
      geo.current.resetSamples();
    }
  }
  const inTour =
    location.pathname === "/" || location.pathname.startsWith("/places/");
  useEffect(() => {
    if (!inTour && gps) setGps(false);
  }, [inTour, gps]);
  useEffect(() => {
    if (!gps || !inTour) return;
    if (!navigator.geolocation) {
      setGpsStatus("unsupported");
      setGpsState(false);
      return;
    }
    setGpsStatus("requesting");
    let granted = false;
    let active = true;
    const watch = navigator.geolocation.watchPosition(
      (position) => {
        if (!active || !tracking.current) return;
        if (!granted) {
          granted = true;
          void track("location_permission_granted");
        }
        const s: Sample = {
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracy: position.coords.accuracy,
          timestamp: position.timestamp,
        };
        setSample(validSample(s, Date.now()) ? s : null);
        setGpsStatus(validSample(s, Date.now()) ? "tracking" : "degraded");
        const p = geo.current.update(
          s,
          live.current.places,
          Date.now(),
          live.current.busy,
        );
        sessionStorage.setItem("sg-geofence", geo.current.snapshot());
        if (p) {
          const place = live.current.places.find((x) => x.id === p.id)!;
          setNearby(place);
          void track("geofence_entered", {
            placeId: p.id,
            languageCode: live.current.locale,
          });
          if (live.current.auto)
            void api<Place>(`/places/${p.id}?language=${live.current.locale}`)
              .then((detail) => play(detail, "auto"))
              .catch(() => {});
        }
      },
      (error) => {
        if (!active || !tracking.current) return;
        setSample(null);
        setGpsStatus(error.code === 1 ? "denied" : "degraded");
        geo.current.resetSamples();
        if (error.code === 1) {
          tracking.current = false;
          setGpsState(false);
          void track("location_permission_denied");
        }
      },
      { enableHighAccuracy: true, maximumAge: 0, timeout: 15000 },
    );
    watcher.current = watch;
    const hide = () => {
      if (document.hidden) setGps(false);
    };
    document.addEventListener("visibilitychange", hide);
    return () => {
      active = false;
      navigator.geolocation.clearWatch(watch);
      if (watcher.current === watch) watcher.current = null;
      document.removeEventListener("visibilitychange", hide);
      geo.current.resetSamples();
    };
  }, [gps, inTour]);
  const geometry = places
    .map((p) => `${p.id}:${p.latitude}:${p.longitude}:${p.triggerRadiusM}`)
    .join("|");
  useEffect(() => {
    geo.current.resetSamples();
  }, [geometry]);
  useEffect(() => {
    if (!sample) return;
    const timer = setTimeout(
      () => {
        setSample(null);
        if (gps) setGpsStatus("degraded");
        geo.current.resetSamples();
      },
      Math.max(0, 15001 - (Date.now() - sample.timestamp)),
    );
    return () => clearTimeout(timer);
  }, [sample, gps]);
  useEffect(() => {
    void refreshCsrf();
  }, [user?.id]);
  return (
    <Context.Provider
      value={{
        locale,
        setLocale,
        user,
        authReady,
        reloadUser,
        places,
        placesLoading,
        placesError,
        reloadPlaces: () => setRevision((n) => n + 1),
        play,
        pause,
        resume,
        stop,
        current,
        audioState,
        audioError,
        elapsed,
        duration,
        gps,
        setGps,
        gpsStatus,
        sample,
        auto,
        setAuto,
        nearby,
        dismissNearby: () => setNearby(null),
      }}
    >
      {children}
    </Context.Provider>
  );
}
