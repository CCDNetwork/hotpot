import { useState } from 'react';
import { Loader2 } from 'lucide-react';
import { useAuth } from '@/providers/GlobalProvider';

export const MetabaseDashboard = () => {
  const { deploymentSettings } = useAuth();
  const [isMetabaseLoaded, setIsMetabaseLoaded] = useState<boolean>(false);

  const onIframeLoad = () => setIsMetabaseLoaded(true);
  return (
    <div className="h-full w-full">
      {!isMetabaseLoaded && (
        <div className="w-full h-full flex flex-col justify-center items-center p-6">
          <Loader2 className="w-10 h-10 animate-spin" />
        </div>
      )}

      <iframe
        onLoad={onIframeLoad}
        className="dark:invert dark:hue-rotate-180"
        src={deploymentSettings?.metabaseUrl}
        width="100%"
        height="100%"
      />
    </div>
  );
};
