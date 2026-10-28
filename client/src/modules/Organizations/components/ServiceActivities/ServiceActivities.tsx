import { useRef, useState } from 'react';
import { Trash2 } from 'lucide-react';
import {
  FieldArrayWithId,
  UseFieldArrayAppend,
  UseFieldArrayRemove,
} from 'react-hook-form';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Tooltip } from '@/components/Tooltip';

interface Props {
  activities: FieldArrayWithId<
    {
      name: string;
      isMpcaActive: boolean;
      isWashActive: boolean;
      isShelterActive: boolean;
      isFoodAssistanceActive: boolean;
      isLivelihoodsActive: boolean;
      isProtectionActive: boolean;
      isIncentivePaymentsActive: boolean;
      activities: {
        title: string;
        serviceType: string;
        id?: string | undefined;
      }[];
    },
    'activities',
    'id'
  >[];
  serviceType: string;
  addActivity: UseFieldArrayAppend<
    {
      name: string;
      isMpcaActive: boolean;
      isWashActive: boolean;
      isShelterActive: boolean;
      isFoodAssistanceActive: boolean;
      isLivelihoodsActive: boolean;
      isProtectionActive: boolean;
      isIncentivePaymentsActive: boolean;
      activities: {
        title: string;
        serviceType: string;
        id?: string | undefined;
      }[];
    },
    'activities'
  >;
  removeActivity: UseFieldArrayRemove;
}

export const ServiceActivities = ({
  activities,
  addActivity,
  removeActivity,
  serviceType,
}: Props) => {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [inputValue, setInputValue] = useState<string>('');

  const handleInputValueChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
  };

  const handleAddBtnClick = () => {
    if (!inputValue) {
      inputRef.current?.focus();
      return;
    }

    addActivity({ title: inputValue, serviceType });
    setInputValue('');
  };

  const filteredActivities = activities.filter(
    (activity) => activity.serviceType === serviceType
  );

  return (
    <div className="animate-opacity">
      {filteredActivities.length > 0 && (
        <div className="pt-4 flex gap-2 flex-col">
          {filteredActivities.map((activity, idx) => (
            <div
              key={activity.id}
              className="flex bg-muted/50 items-center justify-between gap-2 border border-border py-2 px-3 rounded-md animate-opacity"
            >
              <p className="text-sm line-clamp-2">{activity.title}</p>
              <div>
                <Tooltip tooltipContent={'Delete'}>
                  <Button
                    size="icon"
                    onClick={() => removeActivity(idx)}
                    className="h-7 w-7 text-destructive hover:text-red-600"
                    variant="ghost"
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </Tooltip>
              </div>
            </div>
          ))}
        </div>
      )}
      <div className="pt-4">
        <div className="flex gap-4">
          <Input
            type="text"
            placeholder="Add new activity..."
            ref={inputRef}
            value={inputValue}
            onChange={handleInputValueChange}
          />
          <Button type="button" variant="outline" onClick={handleAddBtnClick}>
            Add
          </Button>
        </div>
      </div>
    </div>
  );
};
