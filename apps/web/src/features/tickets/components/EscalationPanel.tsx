import { useState } from 'react';
import { ticketsApi } from '../../../api/tickets';
import { ApiError } from '../../../api/client';
import type { TicketDetail } from '../../../types/api';

const MIN_REASON = 5;
const MAX_REASON = 500;
const MAX_ACTOR = 100;

interface EscalationPanelProps {
  ticket: TicketDetail;
  onEscalated: (updated: TicketDetail) => void;
}

/** The form the agent uses to escalate a ticket. Client validation matches the server. */
export function EscalationPanel({ ticket, onEscalated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [actor, setActor] = useState('');
  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const reasonTrimmed = reason.trim();
  const actorTrimmed = actor.trim();

  const reasonTooShort = reasonTrimmed.length > 0 && reasonTrimmed.length < MIN_REASON;
  const reasonTooLong = reasonTrimmed.length > MAX_REASON;
  const actorTooLong = actorTrimmed.length > MAX_ACTOR;

  const canSubmit =
    reasonTrimmed.length >= MIN_REASON &&
    reasonTrimmed.length <= MAX_REASON &&
    actorTrimmed.length > 0 &&
    actorTrimmed.length <= MAX_ACTOR &&
    !isPending;

  const cannotEscalate = ticket.priority === 'Critical'
    || ticket.status === 'Resolved'
    || ticket.status === 'Closed';

  const disabledReason =
    ticket.priority === 'Critical' ? 'Already Critical.'
    : ticket.status === 'Resolved' ? 'Ticket is Resolved.'
    : ticket.status === 'Closed' ? 'Ticket is Closed.'
    : null;

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    // Client validation: never call the API with invalid input.
    if (!canSubmit) {
      return;
    }

    setIsPending(true);
    setError(null);

    try {
      const updated = await ticketsApi.escalateTicket(ticket.id, {
        reason: reasonTrimmed,
        escalatedBy: actorTrimmed,
      });
      setReason('');
      onEscalated(updated);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Unable to escalate the ticket.');
      }
    } finally {
      setIsPending(false);
    }
  };

  return (
    <div className="card">
      <h2>Escalate</h2>

      {cannotEscalate ? (
        <p className="summary__note" role="note">
          Cannot escalate. {disabledReason}
        </p>
      ) : (
        <form onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label className="field__label" htmlFor="escalate-reason">
              Reason ({MIN_REASON}–{MAX_REASON} characters)
            </label>
            <textarea
              id="escalate-reason"
              rows={3}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              disabled={isPending}
              aria-invalid={reasonTooShort || reasonTooLong}
              aria-describedby="escalate-reason-help"
            />
            <span id="escalate-reason-help" className="summary__note">
              {reasonTrimmed.length} / {MAX_REASON}
            </span>
            {reasonTooShort && (
              <span className="form-error" role="alert">
                Reason must be at least {MIN_REASON} characters.
              </span>
            )}
            {reasonTooLong && (
              <span className="form-error" role="alert">
                Reason must be at most {MAX_REASON} characters.
              </span>
            )}
          </div>

          <div className="field">
            <label className="field__label" htmlFor="escalate-actor">
              Your name
            </label>
            <input
              id="escalate-actor"
              type="text"
              value={actor}
              onChange={(event) => setActor(event.target.value)}
              disabled={isPending}
              aria-invalid={actorTooLong}
            />
            {actorTooLong && (
              <span className="form-error" role="alert">
                Name must be at most {MAX_ACTOR} characters.
              </span>
            )}
          </div>

          {error && (
            <div className="form-error" role="alert">
              {error}
            </div>
          )}

          <button
            type="submit"
            className="button button--primary"
            disabled={!canSubmit}
          >
            {isPending ? 'Escalating…' : 'Escalate'}
          </button>
        </form>
      )}
    </div>
  );
}