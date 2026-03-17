import { redirect } from 'next/navigation'
import Link from 'next/link'
import { getServerSession } from '@/lib/auth'
import { getSeries } from '@/lib/api/series'
import { ApiError } from '@/lib/api/client'
import type { SeriesListItem } from '@/lib/api/types'

const tableWrapperStyle: React.CSSProperties = {
  borderWidth: 'var(--borderWidth-thin, 1px)',
  borderStyle: 'solid',
  borderColor: 'var(--borderColor-default, var(--color-border-default))',
  borderRadius: 'var(--borderRadius-medium, 6px)',
  overflow: 'hidden',
}

const headerRowStyle: React.CSSProperties = {
  borderBottom: 'var(--borderWidth-thin, 1px) solid var(--borderColor-default, var(--color-border-default))',
  backgroundColor: 'var(--bgColor-muted, var(--color-canvas-subtle))',
  color: 'var(--fgColor-muted, var(--color-fg-muted))',
  fontSize: 'var(--text-caption-size, 0.75rem)',
  textTransform: 'uppercase',
  letterSpacing: '0.05em',
}

const bodyRowStyle: React.CSSProperties = {
  borderBottom: 'var(--borderWidth-thin, 1px) solid var(--borderColor-default, var(--color-border-default))',
  transition: 'background-color 0.1s ease',
}

const cellStyle: React.CSSProperties = {
  padding: 'var(--base-size-12, 12px) var(--base-size-16, 16px)',
}

const cellRightStyle: React.CSSProperties = {
  ...cellStyle,
  textAlign: 'right',
  fontVariantNumeric: 'tabular-nums',
}

const titleLinkStyle: React.CSSProperties = {
  color: 'var(--fgColor-default, var(--color-fg-default))',
  fontWeight: 'var(--base-text-weight-medium, 500)',
  textDecoration: 'none',
  borderRadius: 'var(--borderRadius-small, 3px)',
}

const emptyStateStyle: React.CSSProperties = {
  borderWidth: 'var(--borderWidth-thin, 1px)',
  borderStyle: 'solid',
  borderColor: 'var(--borderColor-default, var(--color-border-default))',
  borderRadius: 'var(--borderRadius-medium, 6px)',
  padding: 'var(--base-size-64, 64px) var(--base-size-32, 32px)',
  textAlign: 'center',
  color: 'var(--fgColor-muted, var(--color-fg-muted))',
}

function SeriesTableRow({ item }: { item: SeriesListItem }) {
  return (
    <tr className="group last:[border-bottom:none] hover:bg-[var(--bgColor-muted,var(--color-canvas-subtle))]" style={bodyRowStyle}>
      <td style={cellStyle}>
        <Link
          href={`/series/${item.seriesId}`}
          style={titleLinkStyle}
          className="hover:underline focus:outline-none focus-visible:ring-2 focus-visible:ring-[var(--focus-outlineColor,var(--color-accent-fg))]"
        >
          {item.title}
        </Link>
      </td>
      <td style={cellRightStyle}>{item.sessionCount}</td>
      <td style={cellRightStyle}>{item.totalRegistrations}</td>
      <td style={cellRightStyle}>{item.totalAttendees}</td>
      <td style={cellRightStyle}>{item.uniqueAccountsInfluenced}</td>
      <td style={cellStyle}>{new Date(item.updatedAt).toLocaleDateString()}</td>
    </tr>
  )
}

export default async function SeriesListContent() {
  const session = await getServerSession()

  if (!session?.accessToken) {
    redirect('/api/auth/signin')
  }

  let series: SeriesListItem[]
  try {
    series = await getSeries(session.accessToken)
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      redirect('/api/auth/signin?callbackUrl=%2Fseries')
    }
    throw err
  }

  if (series.length === 0) {
    return (
      <div style={emptyStateStyle}>
        <p style={{ fontSize: 'var(--text-body-size-medium, 0.875rem)' }}>
          No series yet. Create your first local series to start capturing session analytics.
        </p>
      </div>
    )
  }

  return (
    <div style={tableWrapperStyle}>
      <table className="w-full" style={{ fontSize: 'var(--text-body-size-medium, 0.875rem)' }}>
        <thead>
          <tr style={headerRowStyle}>
            <th style={{ ...cellStyle, textAlign: 'left', fontWeight: 'var(--base-text-weight-medium, 500)' }}>Title</th>
            <th style={{ ...cellRightStyle, fontWeight: 'var(--base-text-weight-medium, 500)' }}>Sessions</th>
            <th style={{ ...cellRightStyle, fontWeight: 'var(--base-text-weight-medium, 500)' }}>Registrations</th>
            <th style={{ ...cellRightStyle, fontWeight: 'var(--base-text-weight-medium, 500)' }}>Attendees</th>
            <th style={{ ...cellRightStyle, fontWeight: 'var(--base-text-weight-medium, 500)' }}>Accts Influenced</th>
            <th style={{ ...cellStyle, textAlign: 'left', fontWeight: 'var(--base-text-weight-medium, 500)' }}>Updated</th>
          </tr>
        </thead>
        <tbody>
          {series.map((item) => (
            <SeriesTableRow key={item.seriesId} item={item} />
          ))}
        </tbody>
      </table>
    </div>
  )
}
