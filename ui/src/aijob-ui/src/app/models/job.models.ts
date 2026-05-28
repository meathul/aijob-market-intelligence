export type JobSkillDto = {
  skillId: number;
  skillName?: string | null;
};

export type JobRawDto = {
  id: number;
  title?: string | null;
  company?: string | null;
  location?: string | null;
  description?: string | null;
  salaryRaw?: string | null;
  source?: string | null;
  url?: string | null;
  postedDate?: string; // date-time
  createdAt?: string; // date-time
  isProcessed: boolean;
  skills?: JobSkillDto[] | null;
};

export type JobSearchResultDto = {
  jobs?: JobRawDto[] | null;
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
};
