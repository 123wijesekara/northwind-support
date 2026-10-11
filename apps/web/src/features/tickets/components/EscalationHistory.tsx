import { formatDateTime } from '../../../lib/format';
import type { TicketEscalation } from '../../../types/api';

interface EscalationHistoryProps {
  escalations: TicketEscalation[];
  isLoading?: boolean;
  error?: string | null;
}

/** The immutable list of escalations for a ticket, newest first. */
export function EscalationHistory({ escalations, isLoading, error }: EscalationHistoryProps) {
  if (isLoading) {
    return (
      <div className="card">
        <h2>Escalation history</h2>
        <p className="summary__note">Loading…</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="card">
        <h2>Escalation history</h2>
        <p className="form-error" role="alert">{error}</p>
      </div>
    );
  }

  if (escalations.length === 0) {
    return (
      <div className="card">
        <h2>Escalation history</h2>
        <p className="summary__note">This ticket has not been escalated.</p>
      </div>
    );
  }

  return (
    <div className="card">
      <h2>Escalation history</h2>
      <ol className="escalation-history">
        {escalations.map((row) => (
          <li key={row.id} className="escalation-history__item">
            <div className="escalation-history__header">
              <strong>
                {row.fromPriority} → {row.toPriority}
              </strong>
              <span className="summary__note">{formatDateTime(row.escalatedAtUtc)}</span>
            </div>
            <p className="escalation-history__reason">{row.reason}</p>
            <dl className="escalation-history__meta">
              <div>
                <dt>By</dt>
                <dd>{row.escalatedBy}</dd>
              </div>
              <div>
                <dt>Agent</dt>
                <dd>
                  {row.fromAgent?.fullName ?? 'Unassigned'} → {row.toAgent?.fullName ?? 'Unassigned'}
                </dd>
              </div>
              <div>
                <dt>Due date</dt>
                <dd>
                  {formatDateTime(row.fromDueAtUtc)} → {formatDateTime(row.toDueAtUtc)}
                </dd>
              </div>
            </dl>
          </li>
        ))}
      </ol>
    </div>
  );
}