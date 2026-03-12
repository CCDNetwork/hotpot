import { motion } from 'framer-motion';
import { BadgeXIcon } from 'lucide-react';

import { Button } from '@/components/ui/button';

const variants = {
  hidden: {
    opacity: 0,
    y: 50,
  },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      ease: 'backIn',
      duration: 0.4,
    },
  },
};

interface Props {
  errorMessage: string;
  onOpenChange: () => void;
}

export const PrebookingError = ({ errorMessage, onOpenChange }: Props) => {
  return (
    <motion.section
      className="min-h-[520px] w-full h-full flex flex-col items-center justify-center gap-4 md:gap-2 text-center"
      initial="hidden"
      variants={variants}
      animate="visible"
    >
      <BadgeXIcon
        fill="#ff0000"
        stroke="white"
        strokeWidth={1}
        className="h-40 w-40"
      />
      <h4 className="text-2xl font-semibold md:text-3xl tracking-tight">
        An unexpected error has occcured!
      </h4>
      {errorMessage && (
        <code className="bg-muted px-2 py-1 text-xs rounded-sm text-red-500 whitespace-pre-wrap max-w-[500px] break-words">
          {errorMessage}
        </code>
      )}
      <p className="text-muted-foreground">
        Please close the wizard and try again.
      </p>
      <div className="flex items-center mt-6">
        <Button onClick={onOpenChange} variant="destructive">
          Close
        </Button>
      </div>
    </motion.section>
  );
};
