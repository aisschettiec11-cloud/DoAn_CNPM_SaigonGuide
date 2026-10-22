import { useEffect, useRef, useState } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import type { Place } from "../lib/types";
import type { Sample } from "../lib/geofence";
export function MapView({
  places,
  sample,
  onSelect,
}: {
  places: Place[];
  sample: Sample | null;
  onSelect: (id: string) => void;
}) {
  const element = useRef<HTMLDivElement>(null),
    map = useRef<L.Map | null>(null),
    markers = useRef<L.LayerGroup | null>(null),
    position = useRef<L.LayerGroup | null>(null);
  const select = useRef(onSelect);
  select.current = onSelect;
  const fitted = useRef(false);
  const [error, setError] = useState(false);
  useEffect(() => {
    if (!element.current) return;
    const m = L.map(element.current, {
      zoomControl: false,
      scrollWheelZoom: false,
      minZoom: 11,
      maxBounds: [
        [10.5, 106.4],
        [11, 107],
      ],
      maxBoundsViscosity: 1,
    }).setView([10.7766, 106.6968], 15);
    map.current = m;
    L.control.zoom({ position: "bottomright" }).addTo(m);
    L.tileLayer("/api/v1/tiles/{z}/{x}/{y}.png", {
      attribution:
        '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
    })
      .on("tileerror", () => setError(true))
      .addTo(m);
    markers.current = L.layerGroup().addTo(m);
    position.current = L.layerGroup().addTo(m);
    const resize = new ResizeObserver(() => m.invalidateSize());
    resize.observe(element.current);
    return () => {
      resize.disconnect();
      m.remove();
      map.current = null;
    };
  }, []);
  useEffect(() => {
    if (!map.current || !markers.current) return;
    markers.current.clearLayers();
    places.forEach((p, i) => {
      const icon = L.divIcon({
        className: "place-marker",
        html: `<span>${String(i + 1).padStart(2, "0")}</span>`,
        iconSize: [36, 40],
        iconAnchor: [18, 40],
      });
      const marker = L.marker([p.latitude, p.longitude], {
        icon,
        keyboard: true,
        title: p.name,
      }).addTo(markers.current!);
      const text = document.createElement("span");
      text.textContent = p.name;
      marker.bindTooltip(text, { direction: "top", offset: [0, -36] });
      marker.on("click", () => select.current(p.id));
    });
    if (places.length && !fitted.current) {
      map.current.fitBounds(
        L.latLngBounds(places.map((p) => [p.latitude, p.longitude])),
        { padding: [60, 60] },
      );
      fitted.current = true;
    }
  }, [places]);
  useEffect(() => {
    position.current?.clearLayers();
    if (sample && position.current) {
      L.circle([sample.latitude, sample.longitude], {
        radius: sample.accuracy,
        color: "#4189bb",
        weight: 1,
        fillOpacity: 0.1,
      }).addTo(position.current);
      L.circleMarker([sample.latitude, sample.longitude], {
        radius: 7,
        fillColor: "#4189bb",
        color: "#fff",
        weight: 3,
        fillOpacity: 1,
      }).addTo(position.current);
    }
  }, [sample]);
  return (
    <div className="map-wrap">
      <div
        ref={element}
        className="map-canvas"
        role="region"
        aria-label="Bản đồ các địa điểm; có danh sách thay thế bên cạnh"
      />
      {error && (
        <p className="map-fallback">
          Bản đồ nền đang gián đoạn. Bạn vẫn chọn và đọc các địa điểm từ danh
          sách.
        </p>
      )}
      <div className="map-caption">
        <span className="live-dot" /> HO CHI MINH CITY{" "}
        <span>10.77° N · 106.69° E</span>
      </div>
    </div>
  );
}
