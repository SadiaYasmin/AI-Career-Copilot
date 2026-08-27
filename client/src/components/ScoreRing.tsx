export function ScoreRing({
  value,
  size = 96,
  label,
}: {
  value: number | null | undefined
  size?: number
  label?: string
}) {
  const score = Math.max(0, Math.min(100, value ?? 0))
  const stroke = 8
  const radius = (size - stroke) / 2
  const circumference = 2 * Math.PI * radius
  const offset = circumference - (score / 100) * circumference
  const color = score >= 70 ? '#059669' : score >= 40 ? '#d97706' : '#e11d48'

  return (
    <div className="relative inline-flex items-center justify-center" style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90">
        <circle cx={size / 2} cy={size / 2} r={radius} fill="none" stroke="#e2e8f0" strokeWidth={stroke} />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke={color}
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className="text-2xl font-bold text-slate-900">{score}</span>
        {label && <span className="text-[10px] font-medium uppercase tracking-wide text-slate-400">{label}</span>}
      </div>
    </div>
  )
}