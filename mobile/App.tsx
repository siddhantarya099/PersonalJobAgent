import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, Modal, Pressable, SafeAreaView, StyleSheet, Text, TextInput, useColorScheme, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { Ionicons } from '@expo/vector-icons';
import { analyzeRawJob, createResume, extractCandidateProfile, getApplicationSummary, getApplications, getCurrentResume, getJobs, updateApplicationStatus, type Application, type ApplicationSummary, type ExtractedCandidateProfile, type Job, type MatchResult, type Resume } from './src/api';

export default function App() {
  const systemScheme = useColorScheme();
  const [darkMode, setDarkMode] = useState(systemScheme === 'dark');
  const [activeTab, setActiveTab] = useState('Home');
  const [jobs, setJobs] = useState<Job[]>([]);
  const [focusedJob, setFocusedJob] = useState<Job | null>(null);
  const [description, setDescription] = useState('');
  const [selectedJob, setSelectedJob] = useState<MatchResult | null>(null);
  const [applicationSummary, setApplicationSummary] = useState<ApplicationSummary | null>(null);
  const [applications, setApplications] = useState<Application[]>([]);
  const [resume, setResume] = useState<Resume | null>(null);
  const [resumeDraft, setResumeDraft] = useState('');
  const [resumeLoading, setResumeLoading] = useState(false);
  const [extractedProfile, setExtractedProfile] = useState<ExtractedCandidateProfile | null>(null);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');
  const theme = darkMode ? colors.dark : colors.light;

  useEffect(() => {
    void loadJobs();
    void loadApplicationSummary();
    void loadApplications();
    void loadResume();
  }, []);

  async function loadJobs() {
    try {
      const data = await getJobs();
      setJobs(data.jobs);
      setMessage('');
    } catch {
      setMessage('Start the API on port 5251 to load saved jobs.');
    }
  }

  async function loadApplications() {
    try {
      const data = await getApplications();
      setApplications(data.applications);
    } catch {
      setApplications([]);
    }
  }

  async function loadResume() {
    try {
      setResume(await getCurrentResume());
    } catch {
      setResume(null);
    }
  }

  async function loadApplicationSummary() {
    try {
      setApplicationSummary(await getApplicationSummary());
    } catch {
      setApplicationSummary(null);
    }
  }

  async function analyzeDescription() {
    if (!description.trim()) {
      setMessage('Paste a job description first.');
      return;
    }

    setLoading(true);
    setMessage('');
    try {
      const data = await analyzeRawJob(description);
      setSelectedJob(data.matchResult);
      setDescription('');
      await loadJobs();
      await loadApplicationSummary();
      await loadApplications();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Analysis failed.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <SafeAreaView style={styles.safeArea}>
      <StatusBar style={darkMode ? 'light' : 'dark'} />
      <View style={[styles.container, { backgroundColor: theme.background }]}>
        <View style={styles.headerRow}>
          <View>
            <Text style={[styles.eyebrow, { color: theme.accent }]}>PERSONAL JOB AGENT</Text>
            <Text style={[styles.greeting, { color: theme.muted }]}>Good afternoon</Text>
          </View>
          <Pressable
            accessibilityLabel="Toggle color theme"
            onPress={() => setDarkMode((value) => !value)}
            style={[styles.iconButton, { backgroundColor: theme.card, borderColor: theme.border }]}
          >
            <Ionicons name={darkMode ? 'sunny-outline' : 'moon-outline'} size={19} color={theme.text} />
          </Pressable>
        </View>
        <View style={styles.titleRow}>
          <View style={styles.titleBlock}>
            <Text style={[styles.title, { color: theme.text }]}>Your next role, ranked.</Text>
            <Text style={[styles.subtitle, { color: theme.muted }]}>Here&apos;s what your job agent found.</Text>
          </View>
          <View style={[styles.agentPill, { backgroundColor: theme.successSoft }]}>
            <View style={[styles.statusDot, { backgroundColor: theme.success }]} />
            <Text style={[styles.agentText, { color: theme.success }]}>Agent active</Text>
          </View>
        </View>

        {activeTab === 'Home' ? <>
        <View style={styles.metricsRow}>
          <Metric label="Saved jobs" value={String(jobs.length)} color={theme} />
          <Metric label="Strong matches" value={selectedJob && selectedJob.overallScore >= 85 ? '1' : '0'} color={theme} />
          <Metric label="Action needed" value={String(applicationSummary?.pendingActions ?? 0)} color={theme} />
        </View>

        <View style={[styles.actionBanner, { backgroundColor: theme.card, borderColor: theme.border }]}>
          <View style={[styles.actionIcon, { backgroundColor: theme.accentSoft }]}>
            <Ionicons name="sparkles-outline" size={20} color={theme.accent} />
          </View>
          <View style={styles.actionCopy}>
            <Text style={[styles.actionTitle, { color: theme.text }]}>What should you do right now?</Text>
            <Text style={[styles.actionText, { color: theme.muted }]}>Analyze a role to see your fit and next best action.</Text>
          </View>
        </View>

        <View style={[styles.inputPanel, { backgroundColor: theme.card, borderColor: theme.border }]}>
          <TextInput
            multiline
            value={description}
            onChangeText={setDescription}
            placeholder="Paste a job description..."
            placeholderTextColor="#8b8f98"
            style={[styles.input, { color: theme.text }]}
          />
          <Pressable style={[styles.button, { backgroundColor: theme.text }]} onPress={() => void analyzeDescription()} disabled={loading}>
            {loading ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Analyze job</Text>}
          </Pressable>
        </View>

        {message ? <Text style={[styles.message, { color: theme.accent }]}>{message}</Text> : null}

        {selectedJob ? (
          <View style={[styles.resultPanel, { backgroundColor: theme.successSoft, borderColor: theme.successBorder }]}>
            <Text style={[styles.sectionLabel, { color: theme.success }]}>LATEST MATCH</Text>
            <View style={styles.scoreRow}>
              <Text style={[styles.score, { color: theme.text }]}>{selectedJob.overallScore}%</Text>
              <Text style={[styles.recommendation, { color: theme.success }]}>{selectedJob.recommendation}</Text>
            </View>
            <Text style={[styles.detail, { color: theme.successText }]}>{selectedJob.matchedSkills.length} matched skills · {selectedJob.missingSkills.length} missing</Text>
            {selectedJob.reasons.map((reason) => <Text key={reason} style={[styles.reason, { color: theme.successText }]}>• {reason}</Text>)}
          </View>
        ) : null}

        <View style={styles.jobsHeader}>
          <Text style={[styles.sectionTitle, { color: theme.text }]}>Recommended for you</Text>
          <Text style={[styles.count, { color: theme.accent }]}>{jobs.length} saved</Text>
        </View>
        <FlatList
          data={jobs}
          keyExtractor={(job) => job.id}
          contentContainerStyle={styles.list}
          renderItem={({ item }) => (
            <Pressable accessibilityLabel={`Open ${item.title || 'job'} details`} onPress={() => setFocusedJob(item)} style={[styles.jobCard, { backgroundColor: theme.card, borderColor: theme.border }]}>
              <View style={styles.jobTopRow}>
                <View style={[styles.companyMark, { backgroundColor: theme.accentSoft }]}><Text style={[styles.companyInitial, { color: theme.accent }]}>{(item.company || 'J').slice(0, 1).toUpperCase()}</Text></View>
                <View style={styles.jobHeading}><Text style={[styles.jobTitle, { color: theme.text }]}>{item.title || 'Untitled role'}</Text><Text style={[styles.company, { color: theme.muted }]}>{item.company || 'Company not specified'}</Text></View>
                <Ionicons name="chevron-forward" size={18} color={theme.muted} />
              </View>
              <Text style={[styles.meta, { color: theme.muted }]}>{item.location || 'Location flexible'} · {item.source}</Text>
              {item.salaryMinLpa || item.salaryMaxLpa ? <Text style={[styles.salary, { color: theme.success }]}>{item.salaryMinLpa ?? '?'}–{item.salaryMaxLpa ?? '?'} LPA</Text> : null}
            </Pressable>
          )}
          ListEmptyComponent={<Text style={[styles.empty, { color: theme.muted }]}>No saved jobs yet. Paste a role above to begin.</Text>}
        />
        </> : <SecondaryScreen tab={activeTab} jobs={jobs} applications={applications} summary={applicationSummary} resume={resume} extractedProfile={extractedProfile} resumeDraft={resumeDraft} resumeLoading={resumeLoading} onResumeDraftChange={setResumeDraft} onExtractProfile={async () => { if (!resume) return; setResumeLoading(true); try { setExtractedProfile(await extractCandidateProfile(resume.id)); } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not extract profile.'); } finally { setResumeLoading(false); } }} onSaveResume={async () => { setResumeLoading(true); try { const savedResume = await createResume(resumeDraft); setResume(savedResume); globalThis.localStorage?.setItem('resumeId', savedResume.id); setResumeDraft(''); } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save resume.'); } finally { setResumeLoading(false); } }} onUpdateApplication={async (applicationId, status) => { try { await updateApplicationStatus(applicationId, status); await loadApplications(); await loadApplicationSummary(); } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not update application.'); } }} onJobSelect={setFocusedJob} theme={theme} />}
        <View style={[styles.bottomNav, { backgroundColor: theme.card, borderColor: theme.border }]}>
          {['Home', 'Jobs', 'Applications', 'Resume', 'Agent'].map((tab) => (
            <Pressable key={tab} accessibilityLabel={`${tab} tab`} onPress={() => setActiveTab(tab)} style={styles.navItem}>
              <Ionicons name={tab === 'Home' ? 'home-outline' : tab === 'Jobs' ? 'briefcase-outline' : tab === 'Applications' ? 'layers-outline' : tab === 'Resume' ? 'document-text-outline' : 'sparkles-outline'} size={20} color={activeTab === tab ? theme.accent : theme.muted} />
              <Text style={[styles.navLabel, { color: activeTab === tab ? theme.accent : theme.muted }]}>{tab}</Text>
            </Pressable>
          ))}
        </View>
        <JobDetailsModal job={focusedJob} theme={theme} onClose={() => setFocusedJob(null)} onAnalyze={() => { setFocusedJob(null); setActiveTab('Home'); }} />
      </View>
    </SafeAreaView>
  );
}

function Metric({ label, value, color }: { label: string; value: string; color: typeof colors.light }) {
  return <View style={[styles.metric, { backgroundColor: color.card, borderColor: color.border }]}><Text style={[styles.metricValue, { color: color.text }]}>{value}</Text><Text style={[styles.metricLabel, { color: color.muted }]}>{label}</Text></View>;
}

function SecondaryScreen({ tab, jobs, applications, summary, resume, extractedProfile, resumeDraft, resumeLoading, onResumeDraftChange, onExtractProfile, onSaveResume, onUpdateApplication, onJobSelect, theme }: { tab: string; jobs: Job[]; applications: Application[]; summary: ApplicationSummary | null; resume: Resume | null; extractedProfile: ExtractedCandidateProfile | null; resumeDraft: string; resumeLoading: boolean; onResumeDraftChange: (value: string) => void; onExtractProfile: () => Promise<void>; onSaveResume: () => Promise<void>; onUpdateApplication: (applicationId: string, status: string) => Promise<void>; onJobSelect: (job: Job) => void; theme: typeof colors.light }) {
  const copy = {
    Jobs: {
      icon: 'briefcase-outline' as const,
      eyebrow: 'JOB LIBRARY',
      title: 'Saved jobs',
      body: `${jobs.length} roles are ready for review. Open a job to inspect its match and decide your next move.`,
    },
    Applications: {
      icon: 'layers-outline' as const,
      eyebrow: 'APPLICATIONS',
      title: 'Your pipeline',
      body: summary ? `${summary.total} applications tracked, with ${summary.pendingActions} action${summary.pendingActions === 1 ? '' : 's'} needing attention.` : 'Your application timeline will appear here as you move from shortlist to interview.',
    },
    Resume: {
      icon: 'document-text-outline' as const,
      eyebrow: 'MASTER RESUME',
      title: 'Your source of truth',
      body: 'Keep one factual master resume. Tailored versions will be created from it and never overwrite it.',
    },
    Agent: {
      icon: 'sparkles-outline' as const,
      eyebrow: 'AGENT CONTROL',
      title: 'Quietly working for you',
      body: 'Job discovery is opt-in. Analysis and notifications stay under your control until you enable more automation.',
    },
  }[tab] ?? {
    icon: 'home-outline' as const,
    eyebrow: 'PERSONAL JOB AGENT',
    title: 'Your command center',
    body: 'Everything important will land here.',
  };

  if (tab === 'Applications') {
    return <View style={styles.secondaryScreen}>
      <View style={[styles.secondaryIcon, { backgroundColor: theme.accentSoft }]}><Ionicons name="layers-outline" size={24} color={theme.accent} /></View>
      <Text style={[styles.secondaryEyebrow, { color: theme.accent }]}>APPLICATIONS</Text>
      <Text style={[styles.secondaryTitle, { color: theme.text }]}>Your pipeline</Text>
      <Text style={[styles.secondaryBody, { color: theme.muted }]}>{summary?.total ?? 0} tracked applications · {summary?.pendingActions ?? 0} pending actions</Text>
      <FlatList
        data={applications}
        keyExtractor={(application) => application.id}
        contentContainerStyle={styles.list}
        renderItem={({ item }) => <View style={[styles.applicationCard, { backgroundColor: theme.card, borderColor: theme.border }]}>
          <View style={styles.applicationTopRow}>
            <Text style={[styles.statusBadge, { backgroundColor: theme.accentSoft, color: theme.accent }]}>{applicationStatusLabel(item.status)}</Text>
            <Text style={[styles.applicationId, { color: theme.muted }]}>{item.jobId.slice(0, 8)}</Text>
          </View>
          <Text style={[styles.applicationAction, { color: theme.text }]}>{item.nextAction || 'No pending action'}</Text>
          <Text style={[styles.applicationMeta, { color: theme.muted }]}>{item.events.length} timeline event{item.events.length === 1 ? '' : 's'}</Text>
          {nextApplicationStatus(item.status) ? <Pressable style={[styles.statusButton, { borderColor: theme.border }]} onPress={() => void onUpdateApplication(item.id, nextApplicationStatus(item.status) ?? item.status)}><Text style={[styles.statusButtonText, { color: theme.accent }]}>Move to {nextApplicationStatus(item.status)}</Text></Pressable> : null}
        </View>}
        ListEmptyComponent={<Text style={[styles.empty, { color: theme.muted }]}>No applications yet. Shortlist a job to begin tracking it.</Text>}
      />
    </View>;
  }

  if (tab === 'Jobs') {
    return <View style={styles.secondaryScreen}>
      <View style={[styles.secondaryIcon, { backgroundColor: theme.accentSoft }]}><Ionicons name="briefcase-outline" size={24} color={theme.accent} /></View>
      <Text style={[styles.secondaryEyebrow, { color: theme.accent }]}>JOB LIBRARY</Text>
      <Text style={[styles.secondaryTitle, { color: theme.text }]}>Saved jobs</Text>
      <Text style={[styles.secondaryBody, { color: theme.muted }]}>{jobs.length} roles are ready for review.</Text>
      <FlatList
        data={jobs}
        keyExtractor={(job) => job.id}
        contentContainerStyle={styles.list}
        renderItem={({ item }) => <Pressable accessibilityLabel={`Open ${item.title || 'job'} details`} onPress={() => onJobSelect(item)} style={[styles.jobCard, { backgroundColor: theme.card, borderColor: theme.border }]}>
          <View style={styles.jobTopRow}>
            <View style={[styles.companyMark, { backgroundColor: theme.accentSoft }]}><Text style={[styles.companyInitial, { color: theme.accent }]}>{(item.company || 'J').slice(0, 1).toUpperCase()}</Text></View>
            <View style={styles.jobHeading}><Text style={[styles.jobTitle, { color: theme.text }]}>{item.title || 'Untitled role'}</Text><Text style={[styles.company, { color: theme.muted }]}>{item.company || 'Company not specified'}</Text></View>
            <Ionicons name="chevron-forward" size={18} color={theme.muted} />
          </View>
          <Text style={[styles.meta, { color: theme.muted }]}>{item.location || 'Location flexible'} · {item.source}</Text>
          {item.salaryMinLpa || item.salaryMaxLpa ? <Text style={[styles.salary, { color: theme.success }]}>{item.salaryMinLpa ?? '?'}–{item.salaryMaxLpa ?? '?'} LPA</Text> : null}
        </Pressable>}
        ListEmptyComponent={<Text style={[styles.empty, { color: theme.muted }]}>No jobs yet. Use Home to analyze a role.</Text>}
      />
    </View>;
  }

  if (tab === 'Resume') {
    return <View style={styles.secondaryScreen}>
      <View style={[styles.secondaryIcon, { backgroundColor: theme.accentSoft }]}><Ionicons name="document-text-outline" size={24} color={theme.accent} /></View>
      <Text style={[styles.secondaryEyebrow, { color: theme.accent }]}>MASTER RESUME</Text>
      <Text style={[styles.secondaryTitle, { color: theme.text }]}>Your source of truth</Text>
      <Text style={[styles.secondaryBody, { color: theme.muted }]}>Keep one factual resume. Tailored versions will always be created separately.</Text>
      {resume ? <><View style={[styles.resumeSaved, { backgroundColor: theme.successSoft, borderColor: theme.successBorder }]}><Text style={[styles.comingSoonTitle, { color: theme.success }]}>Master resume active</Text><Text style={[styles.comingSoonBody, { color: theme.successText }]}>Version {resume.versions[0]?.versionNumber ?? 1} · {resume.versions[0]?.source ?? 'User'}</Text></View><Pressable style={[styles.statusButton, { borderColor: theme.border }]} onPress={() => void onExtractProfile()} disabled={resumeLoading}><Text style={[styles.statusButtonText, { color: theme.accent }]}>{resumeLoading ? 'Analyzing resume...' : 'Review extracted profile'}</Text></Pressable>{extractedProfile ? <View style={[styles.resumeSaved, { backgroundColor: theme.card, borderColor: theme.border }]}><Text style={[styles.comingSoonTitle, { color: theme.text }]}>{extractedProfile.currentRole || 'Profile extracted'}</Text><Text style={[styles.comingSoonBody, { color: theme.muted }]}>{extractedProfile.experienceYears} years · {extractedProfile.currentLocation || 'Location not found'}</Text><Text style={[styles.comingSoonBody, { color: theme.muted }]}>{extractedProfile.skills.join(' · ')}</Text></View> : null}</> : <View style={[styles.inputPanel, { backgroundColor: theme.card, borderColor: theme.border }]}><TextInput multiline value={resumeDraft} onChangeText={onResumeDraftChange} placeholder="Paste your master resume..." placeholderTextColor="#8b8f98" style={[styles.input, { color: theme.text }]} /><Pressable style={[styles.button, { backgroundColor: theme.text }]} onPress={() => void onSaveResume()} disabled={resumeLoading || !resumeDraft.trim()}>{resumeLoading ? <ActivityIndicator color="#fff" /> : <Text style={styles.buttonText}>Save master resume</Text>}</Pressable></View>}
    </View>;
  }

  return <View style={styles.secondaryScreen}>
    <View style={[styles.secondaryIcon, { backgroundColor: theme.accentSoft }]}>
      <Ionicons name={copy.icon} size={24} color={theme.accent} />
    </View>
    <Text style={[styles.secondaryEyebrow, { color: theme.accent }]}>{copy.eyebrow}</Text>
    <Text style={[styles.secondaryTitle, { color: theme.text }]}>{copy.title}</Text>
    <Text style={[styles.secondaryBody, { color: theme.muted }]}>{copy.body}</Text>
    <View style={[styles.comingSoon, { backgroundColor: theme.card, borderColor: theme.border }]}>
      <Text style={[styles.comingSoonTitle, { color: theme.text }]}>Next useful step</Text>
      <Text style={[styles.comingSoonBody, { color: theme.muted }]}>{tab === 'Jobs' ? 'Use Home to analyze a new job description.' : 'This view will connect to the next backend workflow.'}</Text>
    </View>
  </View>;
}

function nextApplicationStatus(status: string): string | null {
  const normalizedStatus = applicationStatusLabel(status);
  return { Discovered: 'Shortlisted', Shortlisted: 'ReadyToApply', ReadyToApply: 'Applied', Applied: 'Interview', Assessment: 'Interview', RecruiterContacted: 'Interview', Interview: 'Offer' }[normalizedStatus] ?? null;
}

function JobDetailsModal({ job, theme, onClose, onAnalyze }: { job: Job | null; theme: typeof colors.light; onClose: () => void; onAnalyze: () => void }) {
  return <Modal animationType="slide" transparent visible={job !== null} onRequestClose={onClose}>
    <View style={styles.modalBackdrop}>
      <View style={[styles.modalCard, { backgroundColor: theme.card, borderColor: theme.border }]}>
        <View style={styles.modalHeader}>
          <Text style={[styles.secondaryEyebrow, { color: theme.accent }]}>JOB DETAILS</Text>
          <Pressable accessibilityLabel="Close job details" onPress={onClose} style={[styles.iconButton, { backgroundColor: theme.card, borderColor: theme.border }]}><Ionicons name="close" size={19} color={theme.text} /></Pressable>
        </View>
        <Text style={[styles.modalTitle, { color: theme.text }]}>{job?.title || 'Untitled role'}</Text>
        <Text style={[styles.modalCompany, { color: theme.muted }]}>{job?.company || 'Company not specified'}</Text>
        <View style={styles.modalFacts}>
          <Text style={[styles.modalFact, { color: theme.muted }]}><Ionicons name="location-outline" size={14} color={theme.accent} /> {job?.location || 'Location flexible'}</Text>
          <Text style={[styles.modalFact, { color: theme.muted }]}><Ionicons name="layers-outline" size={14} color={theme.accent} /> {job?.source}</Text>
          {job?.salaryMinLpa || job?.salaryMaxLpa ? <Text style={[styles.modalFact, { color: theme.success }]}><Ionicons name="cash-outline" size={14} color={theme.success} /> {job.salaryMinLpa ?? '?'}–{job.salaryMaxLpa ?? '?'} LPA</Text> : null}
        </View>
        <View style={[styles.modalCallout, { backgroundColor: theme.successSoft, borderColor: theme.successBorder }]}>
          <Text style={[styles.comingSoonTitle, { color: theme.success }]}>Next best action</Text>
          <Text style={[styles.comingSoonBody, { color: theme.successText }]}>Analyze this role to compare it with your verified profile.</Text>
        </View>
        <Pressable style={[styles.button, { backgroundColor: theme.text }]} onPress={onAnalyze}><Text style={styles.buttonText}>Analyze this job</Text></Pressable>
      </View>
    </View>
  </Modal>;
}

function applicationStatusLabel(status: string): string {
  const numericStatus = Number(status);
  return Number.isNaN(numericStatus)
    ? status
    : ({ 1: 'Discovered', 2: 'Shortlisted', 3: 'ReadyToApply', 4: 'Applied', 5: 'Assessment', 6: 'RecruiterContacted', 7: 'Interview', 8: 'Offer', 9: 'Rejected', 10: 'Withdrawn', 11: 'Closed' }[numericStatus] ?? 'Unknown');
}

const colors = {
  light: { background: '#f4f1ea', card: '#fffdf8', text: '#1d2733', muted: '#69727c', border: '#e4ded3', accent: '#c2542d', accentSoft: '#f5ded3', success: '#28745a', successSoft: '#dceee7', successBorder: '#bddbcc', successText: '#315849' },
  dark: { background: '#182029', card: '#222d37', text: '#f3f5f1', muted: '#aab4bc', border: '#35434e', accent: '#ef9a70', accentSoft: '#49342e', success: '#73c7a1', successSoft: '#203d35', successBorder: '#315d4e', successText: '#b5dfce' },
};

const styles = StyleSheet.create({
  safeArea: { flex: 1, backgroundColor: '#f4f1ea' },
  container: { flex: 1, paddingHorizontal: 22, paddingTop: 22 },
  eyebrow: { color: '#c2542d', fontSize: 12, fontWeight: '700', letterSpacing: 1.4 },
  headerRow: { alignItems: 'center', flexDirection: 'row', justifyContent: 'space-between' },
  greeting: { fontSize: 13, marginTop: 5 },
  iconButton: { alignItems: 'center', borderRadius: 20, borderWidth: 1, height: 40, justifyContent: 'center', width: 40 },
  titleRow: { alignItems: 'flex-end', flexDirection: 'row', justifyContent: 'space-between', marginTop: 18 },
  titleBlock: { flex: 1, paddingRight: 10 },
  title: { color: '#1d2733', fontSize: 34, fontWeight: '800', marginTop: 8 },
  subtitle: { color: '#69727c', fontSize: 15, lineHeight: 22, marginTop: 8, marginBottom: 20 },
  agentPill: { alignItems: 'center', borderRadius: 16, flexDirection: 'row', marginBottom: 22, paddingHorizontal: 10, paddingVertical: 7 },
  statusDot: { borderRadius: 5, height: 8, marginRight: 6, width: 8 },
  agentText: { fontSize: 11, fontWeight: '700' },
  metricsRow: { flexDirection: 'row', gap: 8, marginBottom: 14 },
  metric: { borderRadius: 8, borderWidth: 1, flex: 1, padding: 11 },
  metricValue: { fontSize: 22, fontWeight: '800' },
  metricLabel: { fontSize: 11, marginTop: 4 },
  actionBanner: { alignItems: 'center', borderRadius: 8, borderWidth: 1, flexDirection: 'row', marginBottom: 12, padding: 13 },
  actionIcon: { alignItems: 'center', borderRadius: 18, height: 36, justifyContent: 'center', width: 36 },
  actionCopy: { flex: 1, marginLeft: 11 },
  actionTitle: { fontSize: 14, fontWeight: '800' },
  actionText: { fontSize: 12, lineHeight: 17, marginTop: 3 },
  inputPanel: { backgroundColor: '#fffdf8', borderColor: '#e4ded3', borderWidth: 1, borderRadius: 8, padding: 14 },
  input: { minHeight: 88, color: '#1d2733', fontSize: 15, textAlignVertical: 'top' },
  button: { alignItems: 'center', backgroundColor: '#1d2733', borderRadius: 6, justifyContent: 'center', minHeight: 46, marginTop: 12 },
  buttonText: { color: '#fffdf8', fontSize: 15, fontWeight: '700' },
  message: { color: '#b5472a', fontSize: 13, marginTop: 12 },
  resultPanel: { backgroundColor: '#dceee7', borderRadius: 8, marginTop: 18, padding: 16 },
  sectionLabel: { color: '#28745a', fontSize: 11, fontWeight: '800', letterSpacing: 1.2 },
  scoreRow: { alignItems: 'center', flexDirection: 'row', gap: 12, marginTop: 4 },
  score: { color: '#1d2733', fontSize: 34, fontWeight: '800' },
  recommendation: { color: '#28745a', fontSize: 15, fontWeight: '700' },
  detail: { color: '#46665a', fontSize: 13, marginTop: 4 },
  reason: { color: '#315849', fontSize: 13, marginTop: 8 },
  jobsHeader: { alignItems: 'center', flexDirection: 'row', justifyContent: 'space-between', marginTop: 26 },
  sectionTitle: { color: '#1d2733', fontSize: 22, fontWeight: '800' },
  count: { color: '#c2542d', fontSize: 15, fontWeight: '700' },
  list: { gap: 10, paddingBottom: 30, paddingTop: 12 },
  jobCard: { backgroundColor: '#fffdf8', borderColor: '#e4ded3', borderWidth: 1, borderRadius: 8, padding: 15 },
  jobTopRow: { alignItems: 'center', flexDirection: 'row' },
  companyMark: { alignItems: 'center', borderRadius: 20, height: 38, justifyContent: 'center', width: 38 },
  companyInitial: { fontSize: 17, fontWeight: '800' },
  jobHeading: { flex: 1, marginLeft: 10 },
  jobTitle: { color: '#1d2733', fontSize: 17, fontWeight: '700' },
  company: { color: '#4e5965', fontSize: 14, marginTop: 5 },
  meta: { color: '#7d858e', fontSize: 13, marginTop: 8 },
  salary: { color: '#28745a', fontSize: 13, fontWeight: '700', marginTop: 8 },
  empty: { color: '#7d858e', fontSize: 14, paddingVertical: 20 },
  bottomNav: { borderRadius: 10, borderWidth: 1, flexDirection: 'row', justifyContent: 'space-around', marginBottom: 10, marginTop: 10, paddingVertical: 9 },
  navItem: { alignItems: 'center', flex: 1, gap: 3 },
  navLabel: { fontSize: 10, fontWeight: '700' },
  secondaryScreen: { flex: 1, paddingTop: 28 },
  secondaryIcon: { alignItems: 'center', borderRadius: 24, height: 48, justifyContent: 'center', width: 48 },
  secondaryEyebrow: { fontSize: 11, fontWeight: '800', letterSpacing: 1.2, marginTop: 24 },
  secondaryTitle: { fontSize: 30, fontWeight: '800', marginTop: 8 },
  secondaryBody: { fontSize: 15, lineHeight: 23, marginTop: 10, maxWidth: 420 },
  comingSoon: { borderRadius: 8, borderWidth: 1, marginTop: 24, padding: 15 },
  comingSoonTitle: { fontSize: 14, fontWeight: '800' },
  comingSoonBody: { fontSize: 13, lineHeight: 19, marginTop: 5 },
  resumeSaved: { borderRadius: 8, borderWidth: 1, marginTop: 24, padding: 15 },
  applicationCard: { borderRadius: 8, borderWidth: 1, padding: 15 },
  applicationTopRow: { alignItems: 'center', flexDirection: 'row', justifyContent: 'space-between' },
  statusBadge: { borderRadius: 12, fontSize: 11, fontWeight: '800', overflow: 'hidden', paddingHorizontal: 9, paddingVertical: 5 },
  applicationId: { fontSize: 11 },
  applicationAction: { fontSize: 15, fontWeight: '700', marginTop: 14 },
  applicationMeta: { fontSize: 12, marginTop: 8 },
  statusButton: { alignSelf: 'flex-start', borderRadius: 6, borderWidth: 1, marginTop: 14, paddingHorizontal: 10, paddingVertical: 8 },
  statusButtonText: { fontSize: 12, fontWeight: '800' },
  modalBackdrop: { backgroundColor: 'rgba(10, 18, 25, 0.42)', flex: 1, justifyContent: 'flex-end' },
  modalCard: { borderTopLeftRadius: 18, borderTopRightRadius: 18, borderWidth: 1, padding: 22 },
  modalHeader: { alignItems: 'center', flexDirection: 'row', justifyContent: 'space-between' },
  modalTitle: { fontSize: 27, fontWeight: '800', marginTop: 18 },
  modalCompany: { fontSize: 15, marginTop: 5 },
  modalFacts: { gap: 10, marginTop: 22 },
  modalFact: { fontSize: 14 },
  modalCallout: { borderRadius: 8, borderWidth: 1, marginTop: 24, padding: 15 },
});
