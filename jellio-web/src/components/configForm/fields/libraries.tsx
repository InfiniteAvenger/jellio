import { FC } from 'react';
import { UseFormReturn } from 'react-hook-form';
import { Checkbox } from '@/components/ui/checkbox.tsx';
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form.tsx';
import type { Library } from '@/types';

interface Props {
  form: UseFormReturn<any>;
  serverName: string;
  libraries: Library[];
}

export const LibrariesField: FC<Props> = ({ form, serverName, libraries }) => {
  return (
    <FormField
      control={form.control}
      name="libraries"
      render={() => (
        <FormItem className="rounded-lg border p-2">
          <div className="mb-4">
            <FormLabel className="text-base">Catalogs</FormLabel>
            <FormDescription>
              Select which Jellyfin libraries to include in Stremio discovery.
            </FormDescription>
          </div>
          {libraries.length > 0 ? (
            libraries.map((item: Library) => (
              <FormField
                key={item.key}
                control={form.control}
                name="libraries"
                render={({ field }) => {
                  return (
                    <FormItem
                      key={item.key}
                      className="flex flex-row items-start space-x-3 space-y-0"
                    >
                      <FormControl>
                        <Checkbox
                          checked={field.value?.some(
                            (v: Library) => v.key === item.key,
                          )}
                          onCheckedChange={(checked) => {
                            return checked
                              ? field.onChange([
                                  ...(field.value || []),
                                  {
                                    key: item.key,
                                    name: item.name,
                                    type: item.type,
                                  },
                                ])
                              : field.onChange(
                                  field.value?.filter(
                                    (value: any) => value.key !== item.key,
                                  ),
                                );
                          }}
                        />
                      </FormControl>
                      <FormLabel className="font-normal">{`${item.name} | ${serverName}`}</FormLabel>
                    </FormItem>
                  );
                }}
              />
            ))
          ) : (
            <div className="flex flex-col items-center justify-center">
              <div className="w-16 h-16 rounded-full animate-spin border-t-4 border-muted-foreground" />
              <span className="mt-4 text-lg text-muted-foreground text-center">
                No libs :(
              </span>
            </div>
          )}
          <FormMessage />
        </FormItem>
      )}
    />
  );
};
