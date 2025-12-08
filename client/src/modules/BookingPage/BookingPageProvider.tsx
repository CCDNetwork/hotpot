import { createContext, useMemo, ReactNode, useContext, useState } from 'react';
import { Outlet } from 'react-router-dom';

interface BookingContextInterface {
  bookingWizardError: any;
  setBookingWizardError: React.Dispatch<React.SetStateAction<any>>;
}

const BookingContext = createContext<BookingContextInterface>(undefined!);

export const BookingProvider = ({ children = <Outlet /> }: Props) => {
  const [bookingWizardError, setBookingWizardError] = useState<any>(undefined);

  const value = useMemo(
    () => ({ bookingWizardError, setBookingWizardError }),
    [bookingWizardError, setBookingWizardError]
  );

  return (
    <BookingContext.Provider value={value}>{children}</BookingContext.Provider>
  );
};

export const useBookingProvider = () => {
  return useContext(BookingContext);
};

interface Props {
  children?: ReactNode;
}
