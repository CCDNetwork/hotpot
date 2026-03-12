import { ReactNode } from 'react';
import { motion } from 'framer-motion';

type Props = {
  children: ReactNode;
};

const formVariants = {
  hidden: {
    opacity: 0,
    x: 100,
  },
  visible: {
    opacity: 1,
    x: 0,
  },
  exit: {
    opacity: 0,
    x: -50,
    transition: {
      ease: 'easeOut',
    },
  },
};

export const AnimationWrapper = ({ children }: Props) => {
  return (
    <motion.div
      variants={formVariants}
      initial="hidden"
      animate="visible"
      exit="exit"
      transition={{ duration: 0.2 }}
      className="flex items-center justify-center gap-2 max-w-[500px] mx-auto"
    >
      {children}
    </motion.div>
  );
};
