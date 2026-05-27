export type JobDto = {
  id: number;
  title: string;
  company?: string;
  location?: string;
  source?: string;
  url?: string;
  publishedAt?: string;
  minSalary?: number | null;
  maxSalary?: number | null;
  salaryCurrency?: string | null;
  salaryPeriod?: string | null;
  skills?: string[];
};
