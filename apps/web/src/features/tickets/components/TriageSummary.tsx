import { formatDateTime } from '../../../lib/format';
import type { TriageResult } from '../../../types/api';

interface TriageSummaryProps {
  triage: TriageResult;
}

/** Shows what the create-time triage decided and why. */
export function TriageSummary({ triage }: TriageSummaryProps) {
  return (
    <dl className="triage-summary">
      <div>
        <dt>Priority</dt>
        <dd>
          <strong>{triage.priority}</strong>
          <span className="summary__note">{triage.priorityReason}</span>
        </dd>
      </div>

      <div>
        <dt>Response window</dt>
        <dd>
          <strong>{triage.dueAtUtc ? formatDateTime(triage.dueAtUtc) : 'No SLA'}</strong>
          <span className="summary__note">{triage.slaWindowReason}</span>
        </dd>
      </div>

      <div>
        <dt>Assigned to</dt>
        <dd>
          <strong>{triage.assignedAgent?.fullName ?? 'Unassigned'}</strong>
          <span className="summary__note">{triage.assignmentReason}</span>
        </dd>
      </div>
    </dl>
  );
}