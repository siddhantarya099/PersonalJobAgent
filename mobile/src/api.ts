const API_BASE_URL = 'http://192.168.1.8:5251/api';

export type Job = {
  id: string;
  title: string;
  company: string;
  location: string;
  source: string;
  salaryMinLpa?: number;
  salaryMaxLpa?: number;
};

export type JobListResponse = {
  jobs: Job[];
  totalCount: number;
};

export type MatchResult = {
  overallScore: number;
  recommendation: string;
  matchedSkills: string[];
  missingSkills: string[];
  reasons: string[];
};

export type MatchResponse = { matchResult: MatchResult };

export type ApplicationSummary = {
  total: number;
  byStatus: Record<string, number>;
  pendingActions: number;
};

export type Application = {
  id: string;
  jobId: string;
  status: string;
  nextAction: string;
  nextActionDateUtc?: string;
  events: Array<{ description: string; occurredAtUtc: string }>;
};

export type ApplicationListResponse = {
  applications: Application[];
  totalCount: number;
};

export type Resume = {
  id: string;
  name: string;
  versions: Array<{ id: string; versionNumber: number; content: string; source: string; createdAtUtc: string }>;
};

export type ExtractedCandidateProfile = {
  experienceYears: number;
  currentRole: string;
  currentLocation: string;
  skills: string[];
  skillEvidence: Array<{ skill: string; evidence: string }>;
};

async function readJson<T>(response: Response): Promise<T> {
  const data = (await response.json()) as T & { message?: string };
  if (!response.ok) {
    throw new Error(data.message ?? 'The API request failed.');
  }
  return data;
}

export async function getJobs(): Promise<JobListResponse> {
  return readJson(await fetch(`${API_BASE_URL}/job-matching/jobs?page=1&pageSize=20`));
}

export async function getApplicationSummary(): Promise<ApplicationSummary> {
  return readJson(await fetch(`${API_BASE_URL}/applications/summary`));
}

export async function getApplications(): Promise<ApplicationListResponse> {
  return readJson(await fetch(`${API_BASE_URL}/applications?page=1&pageSize=20`));
}

export async function analyzeRawJob(jobDescription: string): Promise<MatchResponse> {
  return readJson(await fetch(`${API_BASE_URL}/job-matching/analyze-raw`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      externalId: `mobile-${Date.now()}`,
      source: 'Mobile',
      company: 'Unknown',
      title: 'Imported role',
      location: '',
      jobUrl: '',
      jobDescription,
    }),
  }));
}

export async function createResume(content: string): Promise<Resume> {
  return readJson(await fetch(`${API_BASE_URL}/resumes`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: 'Master Resume', content, source: 'User' }),
  }));
}

export async function getResume(resumeId: string): Promise<Resume> {
  return readJson(await fetch(`${API_BASE_URL}/resumes/${resumeId}`));
}

export async function getCurrentResume(): Promise<Resume> {
  return readJson(await fetch(`${API_BASE_URL}/resumes/current`));
}

export async function extractCandidateProfile(resumeId: string): Promise<ExtractedCandidateProfile> {
  return readJson(await fetch(`${API_BASE_URL}/resumes/${resumeId}/extract-profile`, { method: 'POST' }));
}

export async function updateApplicationStatus(applicationId: string, status: string, action: string, nextActionDateUtc?: string): Promise<void> {
  await readJson(await fetch(`${API_BASE_URL}/applications/${applicationId}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ 
      status, 
      action,
      nextActionDateUtc,
      source: 'Mobile', 
      description: `Status updated to ${status}` 
    }),
  }));
}
