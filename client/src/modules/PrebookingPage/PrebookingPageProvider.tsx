import { createContext, useMemo, ReactNode, useContext, useState } from 'react';
import { Outlet } from 'react-router-dom';

interface PrebookingContextInterface {
  prebookingWizardError: any;
  setPrebookingWizardError: React.Dispatch<React.SetStateAction<any>>;
}

const PrebookingContext = createContext<PrebookingContextInterface>(undefined!);

export const PrebookingProvider = ({ children = <Outlet /> }: Props) => {
  const [prebookingWizardError, setPrebookingWizardError] =
    useState<any>(undefined);

  const value = useMemo(
    () => ({ prebookingWizardError, setPrebookingWizardError }),
    [prebookingWizardError, setPrebookingWizardError]
  );

  return (
    <PrebookingContext.Provider value={value}>
      {children}
    </PrebookingContext.Provider>
  );
};

export const usePrebookingProvider = () => {
  return useContext(PrebookingContext);
};

interface Props {
  children?: ReactNode;
}
