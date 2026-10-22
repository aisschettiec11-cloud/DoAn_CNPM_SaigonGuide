export type Locale = "vi" | "en";
export type Source = {
  url: string;
  title: string;
  accessedOn: string;
  scope: string;
};
export type Place = {
  id: string;
  code: string;
  name: string;
  address: string;
  shortDescription: string;
  latitude: number;
  longitude: number;
  triggerRadiusM: number;
  imageUrl: string;
  imageAlt: string;
  imageCredit: string;
  imageLicense: string;
  isDemo: boolean;
  verificationStatus: string;
  languageCode: Locale;
  transcript: string | null;
  audioAssetId: string | null;
  durationSeconds: number | null;
  sources: Source[];
};
export type User = {
  id: string;
  email: string;
  displayName: string;
  role: "Admin" | "User";
  status: string;
  preferredLocale: Locale;
  version: number;
  createdAt: string;
};
export type Editor = {
  id?: string;
  code: string;
  latitude: number | null;
  longitude: number | null;
  triggerRadiusM: number;
  status: string;
  verificationStatus: string;
  isDemo: boolean;
  imageUrl: string | null;
  imageCredit: string | null;
  imageLicense: string | null;
  sources: Source[];
  version: number;
  translations: {
    languageCode: Locale;
    name: string;
    address: string;
    shortDescription: string;
    imageAlt: string;
  }[];
  narrations: {
    languageCode: Locale;
    transcript: string;
    sources: Source[];
    audioAssetId: string | null;
  }[];
};
export type Dashboard = {
  range: { from: string; to: string; bucket: string; timeZone: string };
  kpis: {
    pageViews: number;
    sessions: number;
    visitors: number;
    placeViews: number;
    narrationStarts: number;
    narrationCompletions: number;
    registeredUsers: number;
    activeUsers: number;
    completionRate: number | null;
  };
  traffic: { date: string; pageViews: number; sessions: number }[];
  places: {
    id: string;
    name: string;
    status: string;
    views: number;
    sessions: number;
    entries: number;
    starts: number;
    completions: number;
    startRate: number | null;
  }[];
  languages: { code: string; selections: number; starts: number }[];
};
