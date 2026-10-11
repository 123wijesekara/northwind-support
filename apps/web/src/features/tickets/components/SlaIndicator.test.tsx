import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SlaIndicator } from './SlaIndicator';
import type { SlaStatus } from '../../../types/api';

const ALL: SlaStatus[] = ['WithinSla', 'AtRisk', 'Breached', 'Met', 'NotApplicable'];

describe('SlaIndicator', () => {
  it.each(ALL)('renders a distinguishable label for %s', (status) => {
    render(<SlaIndicator slaStatus={status} />);

    const indicator = screen.getByRole('status');
    expect(indicator.getAttribute('aria-label')).toContain('SLA status:');
  });

  it('shows the state name as visible text, not just colour', () => {
    render(<SlaIndicator slaStatus="AtRisk" />);

    expect(screen.getByText('At risk')).toBeTruthy();
  });

  it('shows the due date in full variant', () => {
    render(
      <SlaIndicator slaStatus="WithinSla" dueAtUtc="2026-10-11T15:00:00Z" variant="full" />,
    );

    expect(screen.getByText(/Due/)).toBeTruthy();
  });

  it('does not show the due date in compact variant', () => {
    render(
      <SlaIndicator slaStatus="WithinSla" dueAtUtc="2026-10-11T15:00:00Z" />,
    );

    expect(screen.queryByText(/Due/)).toBeNull();
  });
});