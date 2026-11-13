import { useQueryCache } from "@pinia/colada";
import { mailboxApi, type MailboxId } from "@/api/mailboxes";

export const MAILBOX_KEYS = {
    root: ["mailboxes"] as const,
    detail: (id: MailboxId) => ["mailboxes", id] as const,
    credentials: (id: MailboxId) => ["mailboxes", id, "credentials"] as const,
};

export const mailboxesQuery = {
    key: MAILBOX_KEYS.root,
    query: () => mailboxApi.list(),
};

export function mailboxQuery(id: MailboxId) {
    return { key: MAILBOX_KEYS.detail(id), query: () => mailboxApi.get(id) };
}

export function credentialsQuery(id: MailboxId) {
    return { key: MAILBOX_KEYS.credentials(id), query: () => mailboxApi.credentials(id) };
}

export function useInvalidateMailboxes() {
    const cache = useQueryCache();
    return () => cache.invalidateQueries({ key: MAILBOX_KEYS.root });
}
