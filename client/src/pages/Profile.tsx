import { useEffect, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { api } from '../lib/api'
import type {
  CareerGoalDto,
  CareerLevel,
  CertificationDto,
  EducationDto,
  ExperienceDto,
  ProfileDto,
  ProjectDto,
  SkillDto,
  UpdateProfileCommand,
  WorkType,
} from '../lib/types'
import { Badge, Button, Card, ErrorAlert, Field, Input, PageHeader, Select, Spinner, Textarea } from '../components/ui'
import { useAuth } from '../lib/auth'

const emptyBase = (): UpdateProfileCommand => ({
  fullName: '',
  headline: '',
  phone: '',
  location: '',
  careerLevel: 'MidLevel',
  yearsOfExperience: 0,
  preferredWorkType: 'Hybrid',
  preferredLocation: '',
  targetRole: '',
  targetIndustries: '',
  professionalSummary: '',
  careerGoals: '',
  githubUrl: '',
  linkedInUrl: '',
  portfolioUrl: '',
  education: [],
  experiences: [],
  projects: [],
  skills: [],
  certifications: [],
  goals: [],
  linkedInProfile: null,
})

export function ProfilePage() {
  const { user } = useAuth()
  const [form, setForm] = useState<UpdateProfileCommand | null>(null)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api
      .get<ProfileDto>('/api/profile')
      .then((p) => {
        const cmd: UpdateProfileCommand = {
          fullName: p.fullName || user?.email?.split('@')[0] || '',
          headline: p.headline,
          phone: p.phone,
          location: p.location,
          careerLevel: p.careerLevel,
          yearsOfExperience: p.yearsOfExperience,
          preferredWorkType: p.preferredWorkType,
          preferredLocation: p.preferredLocation,
          targetRole: p.targetRole,
          targetIndustries: p.targetIndustries,
          professionalSummary: p.professionalSummary,
          careerGoals: p.careerGoals,
          githubUrl: p.githubUrl,
          linkedInUrl: p.linkedInUrl,
          portfolioUrl: p.portfolioUrl,
          education: p.education,
          experiences: p.experiences,
          projects: p.projects,
          skills: p.skills,
          certifications: p.certifications,
          goals: p.goals,
          linkedInProfile: p.linkedInProfile,
        }
        setForm(cmd)
      })
      .catch((e) => {
        setError(e instanceof Error ? e.message : 'Failed to load profile')
        setForm(emptyBase())
      })
  }, [user])

  if (!form) return <Spinner label="Loading profile…" />

  function set<K extends keyof UpdateProfileCommand>(key: K, value: UpdateProfileCommand[K]) {
    setForm((f) => (f ? { ...f, [key]: value } : f))
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError('')
    setSaved(false)
    try {
      await api.put<ProfileDto>('/api/profile', form)
      setSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save profile')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div>
      <PageHeader title="Career Profile" subtitle="This powers matches, readiness and interviews" />
      <form onSubmit={onSubmit} className="space-y-6">
        <Card className="p-6">
          <SectionTitle>Basics</SectionTitle>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Full name">
              <Input value={form.fullName} onChange={(e) => set('fullName', e.target.value)} />
            </Field>
            <Field label="Headline">
              <Input value={form.headline} onChange={(e) => set('headline', e.target.value)} placeholder="Senior Backend Engineer" />
            </Field>
            <Field label="Location">
              <Input value={form.location} onChange={(e) => set('location', e.target.value)} />
            </Field>
            <Field label="Phone">
              <Input value={form.phone} onChange={(e) => set('phone', e.target.value)} />
            </Field>
            <Field label="Career level">
              <Select value={form.careerLevel} onChange={(e) => set('careerLevel', e.target.value as CareerLevel)}>
                {['Student', 'FreshGraduate', 'Junior', 'MidLevel', 'Senior', 'Lead', 'Manager'].map((c) => (
                  <option key={c}>{c}</option>
                ))}
              </Select>
            </Field>
            <Field label="Years of experience">
              <Input type="number" min={0} value={form.yearsOfExperience} onChange={(e) => set('yearsOfExperience', Number(e.target.value))} />
            </Field>
            <Field label="Preferred work type">
              <Select value={form.preferredWorkType} onChange={(e) => set('preferredWorkType', e.target.value as WorkType)}>
                {['OnSite', 'Remote', 'Hybrid'].map((w) => (
                  <option key={w}>{w}</option>
                ))}
              </Select>
            </Field>
            <Field label="Preferred location">
              <Input value={form.preferredLocation} onChange={(e) => set('preferredLocation', e.target.value)} placeholder="Remote / Berlin" />
            </Field>
            <Field label="Target role">
              <Input value={form.targetRole} onChange={(e) => set('targetRole', e.target.value)} placeholder="Senior Software Engineer" />
            </Field>
            <Field label="Target industries">
              <Input value={form.targetIndustries} onChange={(e) => set('targetIndustries', e.target.value)} placeholder="Technology, Fintech" />
            </Field>
          </div>
        </Card>

        <Card className="p-6">
          <SectionTitle>Summary & goals</SectionTitle>
          <Field label="Professional summary">
            <Textarea rows={3} value={form.professionalSummary} onChange={(e) => set('professionalSummary', e.target.value)} />
          </Field>
          <div className="mt-4">
            <Field label="Career goals">
              <Textarea rows={2} value={form.careerGoals} onChange={(e) => set('careerGoals', e.target.value)} />
            </Field>
          </div>
          <div className="mt-4 grid gap-4 sm:grid-cols-2">
            <Field label="GitHub">
              <Input value={form.githubUrl} onChange={(e) => set('githubUrl', e.target.value)} />
            </Field>
            <Field label="LinkedIn">
              <Input value={form.linkedInUrl} onChange={(e) => set('linkedInUrl', e.target.value)} />
            </Field>
            <Field label="Portfolio">
              <Input value={form.portfolioUrl} onChange={(e) => set('portfolioUrl', e.target.value)} />
            </Field>
          </div>
        </Card>

        <Card className="p-6">
          <SectionTitle>Skills</SectionTitle>
          <ListEditor<SkillDto>
            items={form.skills}
            onAdd={() => set('skills', [...form.skills, { name: '', category: 'Technical', proficiency: 'Intermediate' }])}
            onRemove={(i) => set('skills', form.skills.filter((_, idx) => idx !== i))}
            onChange={(i, v) => set('skills', form.skills.map((s, idx) => (idx === i ? v : s)))}
            render={(s, setItem) => (
              <div className="grid flex-1 gap-2 sm:grid-cols-[1fr_1fr_1fr]">
                <Input value={s.name} placeholder="Skill (e.g. C#)" onChange={(e) => setItem({ ...s, name: e.target.value })} />
                <Select value={s.category} onChange={(e) => setItem({ ...s, category: e.target.value })}>
                  {['Technical', 'Soft', 'Management', 'Language', 'Other'].map((c) => (
                    <option key={c}>{c}</option>
                  ))}
                </Select>
                <Select value={s.proficiency} onChange={(e) => setItem({ ...s, proficiency: e.target.value })}>
                  {['Beginner', 'Intermediate', 'Advanced', 'Expert'].map((p) => (
                    <option key={p}>{p}</option>
                  ))}
                </Select>
              </div>
            )}
          />
        </Card>

        <Card className="p-6">
          <SectionTitle>Experience</SectionTitle>
          <ListEditor<ExperienceDto>
            items={form.experiences}
            onAdd={() =>
              set('experiences', [
                ...form.experiences,
                { company: '', title: '', location: '', startDate: '', endDate: '', isCurrent: false, description: '', responsibilities: '', achievements: '' },
              ])
            }
            onRemove={(i) => set('experiences', form.experiences.filter((_, idx) => idx !== i))}
            onChange={(i, v) => set('experiences', form.experiences.map((s, idx) => (idx === i ? v : s)))}
            render={(x, setItem) => (
              <div className="grid flex-1 gap-2 sm:grid-cols-2">
                <Input value={x.company} placeholder="Company" onChange={(e) => setItem({ ...x, company: e.target.value })} />
                <Input value={x.title} placeholder="Title" onChange={(e) => setItem({ ...x, title: e.target.value })} />
                <Input value={x.location} placeholder="Location" onChange={(e) => setItem({ ...x, location: e.target.value })} />
                <div className="flex items-center gap-4">
                  <Input value={x.startDate} placeholder="Start (e.g. 2021)" onChange={(e) => setItem({ ...x, startDate: e.target.value })} />
                  {!x.isCurrent && (
                    <Input value={x.endDate} placeholder="End" onChange={(e) => setItem({ ...x, endDate: e.target.value })} />
                  )}
                </div>
                <label className="flex items-center gap-2 text-sm text-slate-600">
                  <input type="checkbox" checked={x.isCurrent} onChange={(e) => setItem({ ...x, isCurrent: e.target.checked, endDate: e.target.checked ? '' : x.endDate })} />
                  Current role
                </label>
                <Textarea rows={2} placeholder="Responsibilities" value={x.responsibilities} onChange={(e) => setItem({ ...x, responsibilities: e.target.value })} />
                <Textarea rows={2} placeholder="Achievements (use numbers)" value={x.achievements} onChange={(e) => setItem({ ...x, achievements: e.target.value })} />
              </div>
            )}
          />
        </Card>

        <Card className="p-6">
          <SectionTitle>Education & projects</SectionTitle>
          <h3 className="mb-2 text-sm font-semibold text-slate-600">Education</h3>
          <ListEditor<EducationDto>
            items={form.education}
            onAdd={() => set('education', [...form.education, { institution: '', degree: '', fieldOfStudy: '', startDate: '', endDate: '', description: '' }])}
            onRemove={(i) => set('education', form.education.filter((_, idx) => idx !== i))}
            onChange={(i, v) => set('education', form.education.map((s, idx) => (idx === i ? v : s)))}
            render={(e, setItem) => (
              <div className="grid flex-1 gap-2 sm:grid-cols-2">
                <Input value={e.institution} placeholder="Institution" onChange={(ev) => setItem({ ...e, institution: ev.target.value })} />
                <Input value={e.degree} placeholder="Degree" onChange={(ev) => setItem({ ...e, degree: ev.target.value })} />
                <Input value={e.fieldOfStudy} placeholder="Field of study" onChange={(ev) => setItem({ ...e, fieldOfStudy: ev.target.value })} />
                <div className="flex gap-2">
                  <Input value={e.startDate} placeholder="Start" onChange={(ev) => setItem({ ...e, startDate: ev.target.value })} />
                  <Input value={e.endDate} placeholder="End" onChange={(ev) => setItem({ ...e, endDate: ev.target.value })} />
                </div>
              </div>
            )}
          />
          <h3 className="mb-2 mt-6 text-sm font-semibold text-slate-600">Projects</h3>
          <ListEditor<ProjectDto>
            items={form.projects}
            onAdd={() => set('projects', [...form.projects, { name: '', description: '', technologies: '', role: '', url: '', startDate: '', endDate: '', highlights: '' }])}
            onRemove={(i) => set('projects', form.projects.filter((_, idx) => idx !== i))}
            onChange={(i, v) => set('projects', form.projects.map((s, idx) => (idx === i ? v : s)))}
            render={(p, setItem) => (
              <div className="grid flex-1 gap-2 sm:grid-cols-2">
                <Input value={p.name} placeholder="Project name" onChange={(ev) => setItem({ ...p, name: ev.target.value })} />
                <Input value={p.technologies} placeholder="Technologies" onChange={(ev) => setItem({ ...p, technologies: ev.target.value })} />
                <Input value={p.role} placeholder="Role" onChange={(ev) => setItem({ ...p, role: ev.target.value })} />
                <Input value={p.url} placeholder="URL" onChange={(ev) => setItem({ ...p, url: ev.target.value })} />
                <Textarea rows={2} placeholder="Description" value={p.description} onChange={(ev) => setItem({ ...p, description: ev.target.value })} />
                <Textarea rows={2} placeholder="Highlights" value={p.highlights} onChange={(ev) => setItem({ ...p, highlights: ev.target.value })} />
              </div>
            )}
          />
        </Card>

        <Card className="p-6">
          <SectionTitle>Certifications & goals</SectionTitle>
          <h3 className="mb-2 text-sm font-semibold text-slate-600">Certifications</h3>
          <ListEditor<CertificationDto>
            items={form.certifications}
            onAdd={() => set('certifications', [...form.certifications, { name: '', issuer: '', dateObtained: '', url: '' }])}
            onRemove={(i) => set('certifications', form.certifications.filter((_, idx) => idx !== i))}
            onChange={(i, v) => set('certifications', form.certifications.map((s, idx) => (idx === i ? v : s)))}
            render={(c, setItem) => (
              <div className="grid flex-1 gap-2 sm:grid-cols-3">
                <Input value={c.name} placeholder="Name" onChange={(ev) => setItem({ ...c, name: ev.target.value })} />
                <Input value={c.issuer} placeholder="Issuer" onChange={(ev) => setItem({ ...c, issuer: ev.target.value })} />
                <Input value={c.dateObtained} placeholder="Year" onChange={(ev) => setItem({ ...c, dateObtained: ev.target.value })} />
              </div>
            )}
          />
          <h3 className="mb-2 mt-6 text-sm font-semibold text-slate-600">Goals</h3>
          <ListEditor<CareerGoalDto>
            items={form.goals}
            onAdd={() => set('goals', [...form.goals, { description: '', timeframe: '' }])}
            onRemove={(i) => set('goals', form.goals.filter((_, idx) => idx !== i))}
            onChange={(i, v) => set('goals', form.goals.map((s, idx) => (idx === i ? v : s)))}
            render={(g, setItem) => (
              <div className="grid flex-1 gap-2 sm:grid-cols-[1fr_200px]">
                <Input value={g.description} placeholder="Goal" onChange={(ev) => setItem({ ...g, description: ev.target.value })} />
                <Input value={g.timeframe} placeholder="Timeframe" onChange={(ev) => setItem({ ...g, timeframe: ev.target.value })} />
              </div>
            )}
          />
        </Card>

        <div className="flex items-center gap-3">
          <Button type="submit" disabled={busy}>
            {busy ? 'Saving…' : 'Save profile'}
          </Button>
          {saved && <Badge color="emerald">Saved</Badge>}
        </div>
        <ErrorAlert message={error} />
      </form>
    </div>
  )
}

function SectionTitle({ children }: { children: ReactNode }) {
  return <h2 className="mb-4 text-base font-semibold text-slate-800">{children}</h2>
}

function ListEditor<T>({
  items,
  render,
  onAdd,
  onRemove,
  onChange,
}: {
  items: T[]
  render: (item: T, setItem: (v: T) => void) => ReactNode
  onAdd: () => void
  onRemove: (index: number) => void
  onChange: (index: number, value: T) => void
}) {
  return (
    <div className="space-y-3">
      {items.length === 0 && <p className="text-sm text-slate-400">Nothing added yet.</p>}
      {items.map((item, i) => (
        <div key={i} className="flex items-start gap-2 rounded-lg border border-slate-200 p-3">
          {render(item, (v) => onChange(i, v))}
          <button
            type="button"
            aria-label="Remove"
            onClick={() => onRemove(i)}
            className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-600"
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      ))}
      <Button type="button" variant="secondary" onClick={onAdd}>
        + Add
      </Button>
    </div>
  )
}