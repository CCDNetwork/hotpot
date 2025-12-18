import { Wand2Icon } from 'lucide-react';

import { PageContainer } from '@/components/PageContainer';
import { Button } from '@/components/ui/button';
import { APP_ROUTE } from '@/helpers/constants';
import { useState } from 'react';
import { BookingWizard } from './components';

export const BookingPage = () => {
  const [isBookingWizardOpen, setIsBookingWizardOpen] =
    useState<boolean>(false);

  const handleBookingWizardOpen = () => {
    setIsBookingWizardOpen(true);
  };

  return (
    <PageContainer
      pageTitle="Booking Cases"
      pageSubtitle="On this page you can run a CWG booking wizard. To run CWG booking, click on the Booking Wizard button below."
      breadcrumbs={[
        {
          href: `${APP_ROUTE.Booking}`,
          name: 'Booking Cases',
        },
      ]}
    >
      <div className="flex justify-center items-center h-[calc(100vh-168px)]">
        <Button type="button" onClick={handleBookingWizardOpen}>
          <Wand2Icon className="mr-2 w-4 h-4" />
          Booking Wizard
        </Button>

        <BookingWizard
          isOpen={isBookingWizardOpen}
          setIsOpen={setIsBookingWizardOpen}
        />
      </div>
    </PageContainer>
  );
};
