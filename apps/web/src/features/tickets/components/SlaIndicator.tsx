import { Badge, type BadgeTone } from '../../../components/Badge';
import { formatDateTime } from '../../../lib/format';
import type { SlaStatus } from '../../../types/api';

/**
 * One indicator for every SLA state. Uses an icon and text so the state is clear
 * without relying on colour. Reused on the ticket list and the ticket detail page.
 */

interface State {
  text: string;
  icon: string;
  tone: BadgeTone;
}

const STATES: Record<SlaStatus, State> = {
  WithinSla: { text: 'Within SLA', icon: '⏱', tone: 'info' },
  AtRisk: { text: 'At risk', icon: '⚠', tone: 'warning' },
  Breached: { text: 'Breached', icon: '⛔', tone: 'danger' },
  Met: { text: 'Met', icon: '✓', tone: 'success' },
  NotApplicable: { text: 'No SLA', icon: '—', tone: 'neutral' },
};

interface SlaIndicatorProps {
  slaStatus: SlaStatus;
  /** Optional: the due date, shown beneath the badge when provided. */
  dueAtUtc?: string | null;
  /** Compact (list) or full (detail) display. Default: compact. */
  variant?: 'compact' | 'full';
}

export function SlaIndicator({ slaStatus, dueAtUtc, variant = 'compact' }: SlaIndicatorProps) {
  const state = STATES[slaStatus];

  return (
    <div className={`sla-indicator sla-indicator--${variant}`}>
      <span
        className="sla-indicator__badge"
        aria-label={`SLA status: ${state.text}`}
        role="status"
      >
        <span aria-hidden="true" className="sla-indicator__icon">
          {state.icon}
        </span>{' '}
        <Badge tone={state.tone}>{state.text}</Badge>
      </span>

      {variant === 'full' && (
        <span className="sla-indicator__due">
          Due {dueAtUtc ? formatDateTime(dueAtUtc) : '—'}
        </span>
      )}
    </div>
  );
}