import { Token } from '@primer/react'
import { MetricsPanel } from '@/components/metrics-panel'
import type { SessionMetricsResponse } from '@/lib/api/types'
import {
  buildPrimarySessionMetricCards,
  buildQaMetricCards,
} from '@/lib/session-analytics'

interface SessionMetricsSectionProps {
  metrics: SessionMetricsResponse | null
}

export function SessionMetricsSection({ metrics }: SessionMetricsSectionProps) {
  if (!metrics) {
    return (
      <section
        className="space-y-2 rounded-lg border p-6"
        style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}
        aria-labelledby="session-metrics-heading"
      >
        <h2 id="session-metrics-heading" className="text-base font-semibold">
          Analytics
        </h2>
        <p
          className="text-sm"
          style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
        >
          Metrics will appear after session data has been imported and processed.
        </p>
      </section>
    )
  }

  const qaCards = buildQaMetricCards(metrics)

  return (
    <section
      className="space-y-4 rounded-lg border p-6"
      style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}
      aria-labelledby="session-metrics-heading"
    >
      <div className="space-y-1">
        <h2 id="session-metrics-heading" className="text-base font-semibold">
          Analytics
        </h2>
        <p
          className="text-sm"
          style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
        >
          Session analytics update as registration, attendance, and Q&A imports are processed.
        </p>
      </div>

      <MetricsPanel metrics={buildPrimarySessionMetricCards(metrics)} />

      {qaCards.length > 0 && (
        <div className="space-y-3">
          <h3 className="text-sm font-semibold">Q&A analytics</h3>
          <MetricsPanel metrics={qaCards} className="sm:grid-cols-3" />
          <p
            className="text-sm"
            style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
          >
            Answered questions: {metrics.answeredQaQuestions.toLocaleString()} of{' '}
            {metrics.totalQaQuestions.toLocaleString()} total.
          </p>
        </div>
      )}

      {metrics.warmAccountsTriggered.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-sm font-semibold">Warm accounts triggered</h3>
          <div className="flex flex-wrap gap-2">
            {metrics.warmAccountsTriggered.map((domain) => (
              <Token key={domain} text={domain} />
            ))}
          </div>
        </div>
      )}
    </section>
  )
}
