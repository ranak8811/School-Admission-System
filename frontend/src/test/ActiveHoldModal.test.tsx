import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ActiveHoldModal } from '../components/ActiveHoldModal';
import { StudentProvider } from '../context/StudentContext';
import { HoldResponse } from '../types/api';

describe('ActiveHoldModal Component', () => {
  const activeHold: HoldResponse = {
    holdId: 'h_test123',
    classId: 'c_science',
    studentId: '20261110000000001',
    status: 'HELD',
    expiresAt: new Date(Date.now() + 600 * 1000).toISOString() // 10 minutes future
  };

  const expiredHold: HoldResponse = {
    holdId: 'h_test456',
    classId: 'c_science',
    studentId: '20261110000000001',
    status: 'EXPIRED',
    expiresAt: new Date(Date.now() - 60 * 1000).toISOString() // Expired 1 min ago
  };

  const renderWithProviders = (ui: React.ReactElement) => {
    return render(<StudentProvider>{ui}</StudentProvider>);
  };

  it('renders active hold details and enables Pay button when hold is active', () => {
    renderWithProviders(
      <ActiveHoldModal
        hold={activeHold}
        isOpen={true}
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText(/Seat Held \(Pending Payment\)/i)).toBeInTheDocument();
    expect(screen.getByText(/Hold ID:\s*h_test123/i)).toBeInTheDocument();

    const payButton = screen.getByRole('button', { name: /Pay Admission Fee/i });
    expect(payButton).toBeInTheDocument();
    expect(payButton).not.toBeDisabled();
  });

  it('renders expired alert and prevents payment when hold is EXPIRED', () => {
    renderWithProviders(
      <ActiveHoldModal
        hold={expiredHold}
        isOpen={true}
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText(/Reservation Window Expired/i)).toBeInTheDocument();

    // Pay button must not be available when expired
    const payButton = screen.queryByRole('button', { name: /Pay Admission Fee/i });
    expect(payButton).toBeNull();

    // Only Close button is available
    const closeButton = screen.getByRole('button', { name: /Close/i });
    expect(closeButton).toBeInTheDocument();
  });

  it('does not render dialog content when isOpen is false', () => {
    const { container } = renderWithProviders(
      <ActiveHoldModal
        hold={activeHold}
        isOpen={false}
        onClose={vi.fn()}
      />
    );

    expect(container).toBeEmptyDOMElement();
  });
});
