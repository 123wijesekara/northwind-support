import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ticketsApi } from '../../../api/tickets';
import { ApiError } from '../../../api/client';
import type { TicketDetail } from '../../../types/api';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    escalateTicket: vi.fn(),
  },
}));

const escalate = vi.mocked(ticketsApi.escalateTicket);

function ticket(overrides: Partial<TicketDetail> = {}): TicketDetail {
  return {
    id: 1,
    reference: 'TCK-0001',
    title: 'Title',
    description: 'Description',
    status: 'Open',
    priority: 'Medium',
    customer: { id: 1, name: 'Contoso', tier: 'Standard', email: 'c@example.com', phone: null },
    category: { id: 1, name: 'General', requiresSpecialist: false },
    assignedAgent: null,
    createdAtUtc: '2026-10-11T10:00:00Z',
    updatedAtUtc: '2026-10-11T10:00:00Z',
    dueAtUtc: '2026-10-12T10:00:00Z',
    resolvedAtUtc: null,
    slaStatus: 'WithinSla',
    ...overrides,
  };
}

describe('EscalationPanel', () => {
  afterEach(() => vi.clearAllMocks());

  it('blocks submission and does not call the API when the reason is too short', () => {
    render(<EscalationPanel ticket={ticket()} onEscalated={vi.fn()} />);

    fireEvent.change(screen.getByLabelText(/Reason/), { target: { value: 'bad' } });

    const button = screen.getByRole('button', { name: /Escalate/ }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);

    fireEvent.click(button);
    expect(escalate).not.toHaveBeenCalled();
  });

  it('does not submit when the reason is empty', () => {
    render(<EscalationPanel ticket={ticket()} onEscalated={vi.fn()} />);

    const button = screen.getByRole('button', { name: /Escalate/ }) as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('submits when the reason is valid and sends only the reason', async () => {
    escalate.mockResolvedValueOnce(ticket({ priority: 'High' }));

    const onEscalated = vi.fn();
    render(<EscalationPanel ticket={ticket()} onEscalated={onEscalated} />);

    fireEvent.change(screen.getByLabelText(/Reason/), { target: { value: 'A valid reason here.' } });

    fireEvent.click(screen.getByRole('button', { name: /Escalate/ }));

    await waitFor(() => {
      expect(escalate).toHaveBeenCalledWith(1, { reason: 'A valid reason here.' });
    });

    expect(onEscalated).toHaveBeenCalled();
  });

  it('shows a 409 from the server without changing the ticket', async () => {
    escalate.mockRejectedValueOnce(
      new ApiError(409, 'Ticket is already Critical and cannot be escalated.'),
    );

    const onEscalated = vi.fn();
    render(<EscalationPanel ticket={ticket()} onEscalated={onEscalated} />);

    fireEvent.change(screen.getByLabelText(/Reason/), { target: { value: 'A valid reason here.' } });

    fireEvent.click(screen.getByRole('button', { name: /Escalate/ }));

    await waitFor(() => {
      expect(screen.getByText(/already Critical/)).toBeTruthy();
    });

    expect(onEscalated).not.toHaveBeenCalled();
  });

  it('renders a "cannot escalate" note when the ticket is Critical', () => {
    render(<EscalationPanel ticket={ticket({ priority: 'Critical' })} onEscalated={vi.fn()} />);

    expect(screen.queryByRole('button', { name: /Escalate/ })).toBeNull();
    expect(screen.getByText(/Already Critical/)).toBeTruthy();
  });

  it('renders a "cannot escalate" note when the ticket is Resolved', () => {
    render(<EscalationPanel ticket={ticket({ status: 'Resolved' })} onEscalated={vi.fn()} />);

    expect(screen.queryByRole('button', { name: /Escalate/ })).toBeNull();
    expect(screen.getByText(/Ticket is Resolved/)).toBeTruthy();
  });
});