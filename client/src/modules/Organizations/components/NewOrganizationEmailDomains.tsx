import { useState } from 'react';
import { XIcon } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { checkEmailDomainForNewOrganization } from '@/services/organizations/emailDomains';

export interface PendingEmailDomain {
  /** Normalised domain, as it will be saved. */
  domain: string;
  /** Existing organisations that already allow this domain. */
  alsoUsedBy: string[];
}

/**
 * Allowed email domains on the Add Organisation form. Each entry is validated
 * by the server as it is added (format, public providers, normalisation) and
 * shows a warning when another organisation already uses it. The domains are
 * saved together with the organisation when the form is submitted.
 */
export const NewOrganizationEmailDomains = ({
  value,
  onChange,
  draft,
  onDraftChange,
}: {
  value: PendingEmailDomain[];
  onChange: (domains: PendingEmailDomain[]) => void;
  /** Text typed but not yet added — lifted so the form can catch it on submit. */
  draft: string;
  onDraftChange: (draft: string) => void;
}) => {
  const [checking, setChecking] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const add = async () => {
    if (!draft.trim()) return;
    setChecking(true);
    setError(null);
    try {
      const result = await checkEmailDomainForNewOrganization(draft);
      if (value.some((d) => d.domain === result.domain)) {
        setError(`'${result.domain}' is already in the list.`);
        return;
      }
      onChange([...value, result]);
      onDraftChange('');
    } catch (e: any) {
      setError(
        e?.response?.data?.errorMessage || 'The domain could not be checked.'
      );
    } finally {
      setChecking(false);
    }
  };

  return (
    <div className="space-y-2 rounded-lg border p-3 shadow-sm">
      <div className="space-y-0.5">
        <div className="text-sm font-medium">Allowed email domains</div>
        <p className="text-xs text-muted-foreground">
          Optional. If you add domains, new users of this organisation can only
          be created with an email address on one of them (exact match). Leave
          empty for organisations whose staff use public email providers such as
          gmail.com.
        </p>
      </div>

      {value.length > 0 && (
        <ul className="space-y-1">
          {value.map((d) => (
            <li key={d.domain} className="rounded-md border px-2 py-1 text-sm">
              <div className="flex items-center gap-2">
                <span className="font-medium">@{d.domain}</span>
                <Button
                  type="button"
                  size="sm"
                  variant="ghost"
                  className="ml-auto h-6 px-1"
                  aria-label={`Remove ${d.domain}`}
                  onClick={() =>
                    onChange(value.filter((x) => x.domain !== d.domain))
                  }
                >
                  <XIcon className="h-4 w-4" />
                </Button>
              </div>
              {d.alsoUsedBy.length > 0 && (
                <p className="text-xs font-medium text-amber-700 dark:text-amber-400">
                  Already allowed for: {d.alsoUsedBy.join(', ')}. Users with
                  this domain could be created in either organisation.
                </p>
              )}
            </li>
          ))}
        </ul>
      )}

      <div className="flex gap-2">
        <Input
          placeholder="example.org"
          value={draft}
          onChange={(e) => {
            onDraftChange(e.target.value);
            setError(null);
          }}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              add();
            }
          }}
        />
        <Button
          type="button"
          variant="outline"
          isLoading={checking}
          disabled={checking || !draft.trim()}
          onClick={add}
        >
          Add
        </Button>
      </div>
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
};
