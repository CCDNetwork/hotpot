import { RefObject } from 'react';
import { Control, UseFieldArrayRemove } from 'react-hook-form';
import { Trash2 } from 'lucide-react';

import { Button } from '@/components/ui/button';
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Tooltip } from '@/components/Tooltip';

import { COUNTRIES_LIST } from '../const';

interface FundingSourceField {
  id: string;
  value: string;
}

interface DeploymentSettingsFieldsProps {
  control: Control<any>;
  fields: FundingSourceField[];
  remove: UseFieldArrayRemove;
  inputValue: string;
  inputRef: RefObject<HTMLInputElement | null>;
  valueAlreadyExists: boolean;
  onInputChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onAddFundingSource: () => void;
}

export const DeploymentSettingsFields = ({
  control,
  fields,
  remove,
  inputValue,
  inputRef,
  valueAlreadyExists,
  onInputChange,
  onAddFundingSource,
}: DeploymentSettingsFieldsProps) => {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          control={control}
          name="deploymentName"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Deployment Name</FormLabel>
              <FormControl>
                <Input placeholder="e.g. CCD Data Portal" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="deploymentCountry"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Deployment Country</FormLabel>
              <FormControl>
                <Select onValueChange={field.onChange} value={field.value}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select country" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {COUNTRIES_LIST.map(({ name, code }) => (
                      <SelectItem key={code} value={name}>
                        {name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          control={control}
          name="adminLevel1Name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Admin Level 1 Name</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Province" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="adminLevel2Name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Admin Level 2 Name</FormLabel>
              <FormControl>
                <Input placeholder="e.g. District" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          control={control}
          name="adminLevel3Name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Admin Level 3 Name</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Sub-district" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="adminLevel4Name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Admin Level 4 Name</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Village" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>
      <FormField
        control={control}
        name="metabaseUrl"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Metabase Iframe URL</FormLabel>
            <FormControl>
              <Input placeholder="URL" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <div>
        <FormLabel>Funding Sources</FormLabel>
        {fields.length > 0 && (
          <div className="pt-4 flex flex-wrap gap-2 w-full">
            {fields.map((activity, idx) => (
              <div
                key={activity.id}
                className="flex w-fit bg-muted/50 items-center justify-between gap-2 border border-border py-1 px-2.5 rounded-md"
              >
                <p className="text-sm line-clamp-2">{activity.value}</p>
                <Tooltip tooltipContent="Remove">
                  <Button
                    size="icon"
                    type="button"
                    onClick={() => remove(idx)}
                    className="h-7 w-7 text-destructive hover:text-red-600"
                    variant="ghost"
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </Tooltip>
              </div>
            ))}
          </div>
        )}
        <div className="pt-4 flex gap-4">
          <div className="flex flex-col w-full">
            <Input
              type="text"
              placeholder="Add a new funding source..."
              ref={inputRef as RefObject<HTMLInputElement>}
              value={inputValue}
              onChange={onInputChange}
            />
            {valueAlreadyExists && (
              <p className="text-sm pt-2 text-red-500">
                Funding source already exists!
              </p>
            )}
          </div>
          <Button type="button" variant="outline" onClick={onAddFundingSource}>
            Add
          </Button>
        </div>
      </div>
    </div>
  );
};
