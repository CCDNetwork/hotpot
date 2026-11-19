import { useState } from 'react';
import { format } from 'date-fns';
import { PencilIcon, Trash2Icon } from 'lucide-react';

import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { toast } from '@/components/ui/use-toast';
import { ConfirmationDialog } from '@/components/ConfirmationDialog';
import {
  checkEmailDomain,
  EmailDomainChange,
  OrganizationEmailDomain,
  useOrganizationEmailDomainHistory,
  useOrganizationEmailDomainMutations,
  useOrganizationEmailDomains,
} from '@/services/organizations/emailDomains';

type PendingChange =
  | { kind: 'add'; domain: string; alsoUsedBy: string[] }
  | {
      kind: 'update';
      id: string;
      from: string;
      domain: string;
      alsoUsedBy: string[];
    }
  | { kind: 'remove'; id: string; domain: string };

const errorMessage = (error: any, fallback: string) =>
  error?.response?.data?.errorMessage || fallback;

const describeChange = (change: EmailDomainChange) => {
  switch (change.action) {
    case 'added':
      return `Added ${change.newValue}`;
    case 'updated':
      return `Changed ${change.oldValue} → ${change.newValue}`;
    case 'removed':
      return change.newValue
        ? `Removed ${change.oldValue} ${change.newValue}`
        : `Removed ${change.oldValue}`;
    default:
      return change.action;
  }
};

/**
 * Allowed email domains for an organisation. When the list is not empty, a
 * new user can only be created in this organisation with an email address on
 * one of these domains (exact match). Existing users are not affected.
 */
export const EmailDomainsSection = ({
  organizationId,
  readOnly = false,
}: {
  organizationId: string;
  /** User Admins see the domains (to know which emails are accepted) but can't change them. */
  readOnly?: boolean;
}) => {
  const { data: domains = [], isLoading } =
    useOrganizationEmailDomains(organizationId);
  const { data: history = [] } =
    useOrganizationEmailDomainHistory(organizationId);
  const { addDomain, updateDomain, removeDomain } =
    useOrganizationEmailDomainMutations(organizationId);

  const [newDomain, setNewDomain] = useState('');
  const [editing, setEditing] = useState<{ id: string; value: string } | null>(
    null
  );
  const [pending, setPending] = useState<PendingChange | null>(null);
  const [checking, setChecking] = useState(false);

  const validate = async (
    domain: string,
    onValid: (normalised: string, alsoUsedBy: string[]) => void,
    excludeDomainId?: string
  ) => {
    setChecking(true);
    try {
      const result = await checkEmailDomain(
        organizationId,
        domain,
        excludeDomainId
      );
      onValid(result.domain, result.alsoUsedBy);
    } catch (error: any) {
      toast({
        title: 'Domain not accepted',
        variant: 'destructive',
        description: errorMessage(error, 'The domain could not be checked.'),
      });
    } finally {
      setChecking(false);
    }
  };

  const onAddClick = () =>
    validate(newDomain, (domain, alsoUsedBy) =>
      setPending({ kind: 'add', domain, alsoUsedBy })
    );

  const onSaveEditClick = (current: OrganizationEmailDomain) => {
    if (!editing) return;
    validate(
      editing.value,
      (domain, alsoUsedBy) => {
        if (domain === current.domain) {
          setEditing(null);
          return;
        }
        setPending({
          kind: 'update',
          id: current.id,
          from: current.domain,
          domain,
          alsoUsedBy,
        });
      },
      current.id
    );
  };

  const onConfirm = async () => {
    if (!pending) return;
    try {
      if (pending.kind === 'add') {
        await addDomain.mutateAsync(pending.domain);
        setNewDomain('');
      } else if (pending.kind === 'update') {
        await updateDomain.mutateAsync({
          id: pending.id,
          domain: pending.domain,
        });
        setEditing(null);
      } else {
        await removeDomain.mutateAsync(pending.id);
      }
      toast({ title: 'Allowed email domains updated' });
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description: errorMessage(error, 'The change was not saved.'),
      });
    } finally {
      setPending(null);
    }
  };

  const dialog = (() => {
    if (!pending) return null;
    if (pending.kind === 'add')
      return {
        title: `Add ${pending.domain}?`,
        body: (
          <>
            New users of this organisation will need an email address ending in{' '}
            <strong>@{pending.domain}</strong>
            {domains.length > 0 ? ' or another domain on the list' : ''}. Check
            the spelling: a mistyped domain blocks new users until it is
            corrected. Existing users are not affected.
          </>
        ),
        label: 'Add domain',
        variant: 'default' as const,
      };
    if (pending.kind === 'update')
      return {
        title: `Change ${pending.from} to ${pending.domain}?`,
        body: (
          <>
            New users with an <strong>@{pending.from}</strong> address will no
            longer be accepted; <strong>@{pending.domain}</strong> addresses
            will be. Existing users are not affected.
          </>
        ),
        label: 'Save change',
        variant: 'default' as const,
      };
    return {
      title: `Remove ${pending.domain}?`,
      body:
        domains.length === 1 ? (
          <>
            This is the last domain on the list.{' '}
            <strong>
              The organisation will become unrestricted: new users can be
              created with any email address.
            </strong>{' '}
            Existing users are not affected.
          </>
        ) : (
          <>
            New users with an <strong>@{pending.domain}</strong> address will no
            longer be accepted. Existing users are not affected.
          </>
        ),
      label: 'Remove domain',
      variant: 'destructive' as const,
    };
  })();

  return (
    <Card className="sm:bg-secondary/10 border-0 sm:border sm:dark:bg-secondary/10 shadow-none">
      <CardHeader>
        <CardTitle>Allowed email domains</CardTitle>
        <CardDescription>
          {domains.length === 0
            ? 'No domains configured: users can be created with any email address. Add a domain to restrict new users to that domain.'
            : 'New users can only be created with an email address on one of these domains (exact match — subdomains must be added separately). Existing users are not affected.'}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {!isLoading && domains.length > 0 && (
          <ul className="divide-y rounded-md border">
            {domains.map((d) => (
              <li
                key={d.id}
                className="flex flex-wrap items-center gap-2 px-3 py-2"
              >
                {editing?.id === d.id ? (
                  <>
                    <Input
                      className="h-8 max-w-xs"
                      value={editing.value}
                      onChange={(e) =>
                        setEditing({ id: d.id, value: e.target.value })
                      }
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault();
                          onSaveEditClick(d);
                        }
                      }}
                      autoFocus
                    />
                    <Button
                      type="button"
                      size="sm"
                      isLoading={checking}
                      disabled={checking || !editing.value.trim()}
                      onClick={() => onSaveEditClick(d)}
                    >
                      Save
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      onClick={() => setEditing(null)}
                    >
                      Cancel
                    </Button>
                  </>
                ) : (
                  <>
                    <span className="font-medium">@{d.domain}</span>
                    {d.alsoUsedBy.length > 0 && (
                      // Only for domains shared before this was blocked: a
                      // domain can belong to only one organisation.
                      <Badge
                        variant="destructive"
                        title={`Also allowed for: ${d.alsoUsedBy.join(', ')}. A domain can belong to only one organisation; remove it from one of them.`}
                      >
                        also on {d.alsoUsedBy.join(', ')}
                      </Badge>
                    )}
                    {!readOnly && (
                      <div className="ml-auto flex gap-1">
                        <Button
                          type="button"
                          size="sm"
                          variant="ghost"
                          aria-label={`Edit ${d.domain}`}
                          onClick={() =>
                            setEditing({ id: d.id, value: d.domain })
                          }
                        >
                          <PencilIcon className="h-4 w-4" />
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="ghost"
                          aria-label={`Remove ${d.domain}`}
                          onClick={() =>
                            setPending({
                              kind: 'remove',
                              id: d.id,
                              domain: d.domain,
                            })
                          }
                        >
                          <Trash2Icon className="h-4 w-4" />
                        </Button>
                      </div>
                    )}
                  </>
                )}
              </li>
            ))}
          </ul>
        )}

        {!readOnly && (
          <>
            <div className="flex gap-2">
              <Input
                placeholder="example.org"
                value={newDomain}
                onChange={(e) => setNewDomain(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && newDomain.trim()) {
                    e.preventDefault();
                    onAddClick();
                  }
                }}
              />
              <Button
                type="button"
                variant="outline"
                isLoading={checking && !editing}
                disabled={checking || !newDomain.trim()}
                onClick={onAddClick}
              >
                Add domain
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">
              Public email providers (gmail.com, outlook.com, yahoo.com, …)
              cannot be added. Organisations whose staff use them should have no
              domain list.
            </p>
          </>
        )}

        {history.length > 0 && (
          <>
            <Separator />
            <div className="space-y-2">
              <div className="text-sm font-medium">Change history</div>
              <ul className="space-y-1 text-sm">
                {history.map((change) => (
                  <li
                    key={change.id}
                    className="flex flex-wrap gap-x-2 text-muted-foreground"
                  >
                    <span className="tabular-nums">
                      {format(new Date(change.createdAt), 'dd/MM/yyyy HH:mm')}
                    </span>
                    <span className="text-foreground">
                      {describeChange(change)}
                    </span>
                    {change.performedByEmail && (
                      <span>by {change.performedByEmail}</span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          </>
        )}
      </CardContent>

      {dialog && (
        <ConfirmationDialog
          open={!!pending}
          title={dialog.title}
          body={dialog.body}
          confirmButtonLabel={dialog.label}
          actionButtonVariant={dialog.variant}
          confirmButtonLoading={
            addDomain.isLoading ||
            updateDomain.isLoading ||
            removeDomain.isLoading
          }
          onCancel={() => setPending(null)}
          onAction={onConfirm}
        />
      )}
    </Card>
  );
};
