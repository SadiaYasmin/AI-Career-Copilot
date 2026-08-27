import { useAuth } from '../lib/auth'
import { Badge, Button, Card, PageHeader } from '../components/ui'
import { Link } from 'react-router-dom'

export function SettingsPage() {
  const { user, logout } = useAuth()

  return (
    <div>
      <PageHeader title="Settings" subtitle="Manage your account" />
      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="p-6">
          <h2 className="mb-4 text-sm font-semibold text-slate-700">Account</h2>
          <dl className="space-y-3 text-sm">
            <div className="flex items-center justify-between">
              <dt className="text-slate-400">Email</dt>
              <dd className="font-medium text-slate-700">{user?.email}</dd>
            </div>
            <div className="flex items-center justify-between">
              <dt className="text-slate-400">Role</dt>
              <dd>
                <Badge>{user?.role}</Badge>
              </dd>
            </div>
            <div className="flex items-center justify-between">
              <dt className="text-slate-400">Status</dt>
              <dd>{user?.isActive ? <Badge color="emerald">Active</Badge> : <Badge color="rose">Inactive</Badge>}</dd>
            </div>
            <div className="flex items-center justify-between">
              <dt className="text-slate-400">User ID</dt>
              <dd className="max-w-[55%] truncate font-mono text-xs text-slate-500">{user?.id}</dd>
            </div>
          </dl>
        </Card>

        <Card className="p-6">
          <h2 className="mb-4 text-sm font-semibold text-slate-700">Data you manage</h2>
          <ul className="space-y-2 text-sm text-slate-600">
            <li><Link to="/profile" className="text-indigo-600 hover:text-indigo-700">Profile</Link> — career details powering matches</li>
            <li><Link to="/resumes" className="text-indigo-600 hover:text-indigo-700">Resumes</Link> — uploads, analysis and defaults</li>
            <li><Link to="/jobs" className="text-indigo-600 hover:text-indigo-700">Jobs</Link> — job postings and analysis</li>
            <li><Link to="/applications" className="text-indigo-600 hover:text-indigo-700">Applications</Link> — pipeline tracking</li>
            <li><Link to="/copilot" className="text-indigo-600 hover:text-indigo-700">Copilot chats</Link> — conversation history</li>
          </ul>
        </Card>

        <Card className="p-6 lg:col-span-2">
          <h2 className="mb-2 text-sm font-semibold text-slate-700">Sign out</h2>
          <p className="mb-4 text-sm text-slate-500">End this session and return to the login screen.</p>
          <Button variant="secondary" onClick={() => logout()}>
            Sign out
          </Button>
        </Card>
      </div>
    </div>
  )
}