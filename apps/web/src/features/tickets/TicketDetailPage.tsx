import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ticketsApi } from '../../api/tickets';
import { ErrorMessage } from '../../components/ErrorMessage';
import { Spinner } from '../../components/Spinner';
import { useReferenceData } from '../../hooks/useReferenceData';
import type { TicketEscalation } from '../../types/api';
import { AssignAgentControl } from './components/AssignAgentControl';
import { EscalationHistory } from './components/EscalationHistory';
import { EscalationPanel } from './components/EscalationPanel';
import { StatusActions } from './components/StatusActions';
import { TicketSummary } from './components/TicketSummary';
import { useTicket } from './hooks/useTicket';

export function TicketDetailPage() {
  const { id } = useParams<{ id: string }>();
  const ticketId = Number(id);

  const { data: ticket, isLoading, error, reload, setData } = useTicket(ticketId);
  const reference = useReferenceData();

  const [escalations, setEscalations] = useState<TicketEscalation[]>([]);
  const [escalationsError, setEscalationsError] = useState<string | null>(null);
  const [escalationsLoading, setEscalationsLoading] = useState(true);

  // Load escalation history whenever the ticket changes.
  useEffect(() => {
    if (Number.isNaN(ticketId)) {
      return;
    }

    let cancelled = false;

    const load = async () => {
      setEscalationsLoading(true);
      setEscalationsError(null);

      try {
        const data = await ticketsApi.getEscalations(ticketId);
        if (!cancelled) {
          setEscalations(data);
        }
      } catch (err) {
        if (!cancelled) {
          setEscalationsError('Unable to load escalation history.');
        }
      } finally {
        if (!cancelled) {
          setEscalationsLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [ticketId, ticket?.updatedAtUtc]);

  if (Number.isNaN(ticketId)) {
    return <ErrorMessage message="That is not a valid ticket reference." />;
  }

  if (isLoading && !ticket) {
    return <Spinner label="Loading ticket" />;
  }

  if (error && !ticket) {
    return <ErrorMessage message={error} onRetry={() => void reload()} />;
  }

  if (!ticket) {
    return null;
  }

  const handleEscalated = (updated: typeof ticket) => {
    setData(updated);
    // History reload is triggered by the useEffect above, watching updatedAtUtc.
  };

  return (
    <section>
      <header className="page-header">
        <div>
          <p className="breadcrumb">
            <Link to="/tickets">Tickets</Link> / {ticket.reference}
          </p>
          <h1>{ticket.title}</h1>
        </div>
      </header>

      {error && <ErrorMessage message={error} onRetry={() => void reload()} />}

      <div className="detail-layout">
        <div className="detail-layout__main">
          <div className="card">
            <h2>Description</h2>
            <p className="description">{ticket.description}</p>
          </div>

          <StatusActions ticket={ticket} onUpdated={setData} />
        </div>

        <aside className="detail-layout__side">
          <TicketSummary ticket={ticket} />

          <AssignAgentControl
            ticket={ticket}
            agents={reference.data?.agents ?? []}
            onUpdated={setData}
          />

          <EscalationPanel ticket={ticket} onEscalated={handleEscalated} />

          <EscalationHistory
            escalations={escalations}
            isLoading={escalationsLoading}
            error={escalationsError}
          />
        </aside>
      </div>
    </section>
  );
}