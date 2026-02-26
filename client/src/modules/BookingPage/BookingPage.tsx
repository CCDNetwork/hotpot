import {
  DownloadIcon,
  FileDownIcon,
  FileTextIcon,
  Wand2Icon,
} from 'lucide-react';

import { PageContainer } from '@/components/PageContainer';
import { Button } from '@/components/ui/button';
import { APP_ROUTE } from '@/helpers/constants';
import { useState } from 'react';
import { BookingWizard } from './components';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

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
      headerNode={
        <DropdownMenu>
          <DropdownMenuTrigger>
            <Button type="button">
              <DownloadIcon className="mr-2 w-4 h-4" />
              Download template
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            <DropdownMenuItem className="p-0">
              <a
                href="/booking-empty-upload-template-1.xlsx"
                download
                className="flex gap-1 items-center px-2 py-1"
              >
                <FileDownIcon className="w-4 h-4" />
                Empty template
              </a>
            </DropdownMenuItem>
            <DropdownMenuItem className="p-0">
              <a
                href="/booking-template-with-readme-1.xlsx"
                download
                className="flex gap-1 items-center px-2 py-1"
              >
                <FileTextIcon className="w-4 h-4" />
                Template with readme
              </a>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      }
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
