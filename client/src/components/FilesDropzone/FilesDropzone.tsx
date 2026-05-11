import { useCallback, useMemo } from 'react';
import { Control, useController } from 'react-hook-form';
import {
  DownloadIcon,
  LucideLoader2,
  Paperclip,
  PlusIcon,
  Trash2Icon,
  UploadCloudIcon,
} from 'lucide-react';
import { useDropzone, FileRejection } from 'react-dropzone';

import { Button } from '@/components/ui/button';
import { activeStorageTypeId } from '@/services/storage/config';
import { useStorageFileMutation } from '@/services/storage/api';
import { cn } from '@/helpers/utils';

import { determineFileType } from './helpers';
import { toast } from '../ui/use-toast';

interface Props {
  name: string;
  control: Control<any, any>;
  maxFiles?: number;
  disabled?: boolean;
}

export const FilesDropzone = ({ name, control, maxFiles, disabled }: Props) => {
  const { field } = useController({
    control,
    name,
  });

  const { addStorageFile } = useStorageFileMutation();

  const onDrop = useCallback(
    async (acceptedFiles: File[], rejectedFiles: FileRejection[]) => {
      if (
        maxFiles === 1 &&
        rejectedFiles.find((file) =>
          file.errors.find((err) => err.code === 'too-many-files')
        )
      ) {
        toast({
          title: 'Files limit error!',
          variant: 'destructive',
          description: 'Only one file can be uploaded!',
        });
        return;
      }

      if (acceptedFiles && acceptedFiles.length) {
        const files = await Promise.all(
          acceptedFiles.map(async (file: File) => {
            return {
              id: -1,
              name: file.name,
              url: URL.createObjectURL(file),
              buffer: await file.arrayBuffer(),
            };
          })
        );

        const filesToUpload = [] as any[];

        await Promise.all(
          files.map(async (file) => {
            const res = await addStorageFile.mutateAsync({
              file,
              type: activeStorageTypeId,
            });

            filesToUpload.push(res);
          })
        );

        if (maxFiles && maxFiles === 1 && filesToUpload.length === 1) {
          field.onChange(filesToUpload[0]);
        } else {
          field.onChange([...field.value, ...filesToUpload]);
        }
        toast({
          title: 'Success',
          variant: 'default',
          description: `${filesToUpload.length === 1 ? 'File' : 'Files'} successfully uploaded`,
        });
      }

      if (rejectedFiles && rejectedFiles.length) {
        toast({
          title: 'Max size error!',
          variant: 'destructive',
          description: 'Maximum file size is 10MB',
        });
      }
    },
    [addStorageFile, field, maxFiles]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {},
    maxFiles: maxFiles || 0,
    multiple: !maxFiles || maxFiles > 1,
    maxSize: 10485760, // 10MB
    disabled: disabled,
  });

  const removeClicked = (ev: any, index: number) => {
    ev.preventDefault();

    field.onChange([
      ...field.value.slice(0, index),
      ...field.value.slice(index + 1),
    ]);
  };

  const singleImageRemoveClicked = () => {
    field.onChange(null);
  };

  const renderFileDropzone = useMemo(
    () => (
      <div
        {...getRootProps()}
        className={cn(
          'border-2 border-dashed bg-muted/20 rounded-xl flex items-center justify-center p-6 text-center transition-colors duration-300  hover:cursor-pointer',
          { 'bg-primary/5': isDragActive },
          {
            'hover:duration-0 hover:cursor-not-allowed text-gray-400': disabled,
          },
          { 'hover:border-primary': !disabled }
        )}
      >
        <input {...getInputProps()} />
        {!addStorageFile.isLoading &&
          (!field.value.length ? (
            <div className="flex items-center gap-1 flex-col text-xs">
              <UploadCloudIcon
                className={cn('w-12 h-12 stroke-1 text-muted-foreground', {
                  'text-gray-400': disabled,
                })}
              />
              <div className="inline-flex gap-1">
                <p className="font-medium underline">Click to choose a file</p>
                <p>or</p>
              </div>
              <p>drag & drop it here</p>
            </div>
          ) : (
            <div className="aspect-square py-2">
              <PlusIcon className="w-10 h-10 text-muted-foreground" />
            </div>
          ))}
        {addStorageFile.isLoading && (
          <div className="py-2">
            <LucideLoader2 className="w-10 h-10 animate-spin" />
          </div>
        )}
      </div>
    ),
    [
      getRootProps,
      isDragActive,
      disabled,
      getInputProps,
      addStorageFile.isLoading,
      field.value.length,
    ]
  );

  return (
    <div
      className={cn(
        'grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 rounded-md relative gap-4',
        {
          'grid-cols-1 sm:grid-cols-1 lg:grid-cols-1':
            (!maxFiles || maxFiles > 1) && field.value?.length === 0,
        }
      )}
    >
      {maxFiles && maxFiles === 1 && field.value && (
        <div className="relative rounded-lg aspect-square overflow-hidden primary-500 flex-shrink-0 group">
          <img
            src={field.value?.url}
            className="object-cover h-full w-full"
            alt={field.value?.alt}
          />
          <div className="absolute top-1 right-1 hidden group-hover:block group">
            <Button
              className="w-8 h-8 p-0 rounded-3xl"
              type="button"
              size="icon"
              onClick={singleImageRemoveClicked}
              variant="destructive"
            >
              <Trash2Icon className="w-4 h-4" />
            </Button>
          </div>
        </div>
      )}

      {(!maxFiles || maxFiles > 1) &&
        field.value?.map((file: any, index: number) => {
          const fileType = determineFileType(file.url);
          return (
            <div
              className="relative rounded-xl aspect-square overflow-hidden flex-shrink-0 group bg-muted/50"
              key={file.id}
            >
              {fileType === 'image' ? (
                <img
                  src={file?.url}
                  className="object-cover h-full w-full"
                  alt={file?.alt}
                  loading="lazy"
                />
              ) : (
                <div className="p-6 w-full h-full text-center items-center justify-center flex flex-col break-word text-xs">
                  <Paperclip className="size-5 mb-2 shrink-0" />
                  <span>{file.name}</span>
                </div>
              )}
              <div className="absolute top-1 right-1 hidden group-hover:block group w-[93%]">
                <div className="inline-flex justify-between w-full">
                  <a
                    href={file.url}
                    download={file.name}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    <Button
                      className="w-6 h-6 rounded-full"
                      type="button"
                      size="icon"
                    >
                      <DownloadIcon className="w-4 h-4" />
                    </Button>
                  </a>
                  <Button
                    className="w-6 h-6 rounded-full"
                    type="button"
                    size="icon"
                    onClick={(ev) => removeClicked(ev, index)}
                    variant="destructive"
                  >
                    <Trash2Icon className="w-4 h-4" />
                  </Button>
                </div>
              </div>
            </div>
          );
        })}
      {renderFileDropzone}
    </div>
  );
};
