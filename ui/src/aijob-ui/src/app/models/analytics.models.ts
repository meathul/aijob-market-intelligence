export type TimeSeriesPointDto = {
  day: string; // date
  count: number;
};

export type KeyValueCountDto = {
  key?: string | null;
  count: number;
};

export type SalaryStatsDto = {
  min?: number | null;
  max?: number | null;
  avg?: number | null;
  currency?: string | null;
  location?: string | null;
  experienceLevel?: string | null;
  postedWithinDays?: number | null;
};
