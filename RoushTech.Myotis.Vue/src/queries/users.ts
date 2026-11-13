import { useQueryCache } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import { userApi } from "@/api/users";
import { MAILBOX_KEYS } from "@/queries/mailboxes";

export const USER_KEYS = {
    root: ["users"] as const,
    people: (mailboxId: MailboxId) => [...MAILBOX_KEYS.detail(mailboxId), "people"] as const,
};

export const usersQuery = {
    key: USER_KEYS.root,
    query: () => userApi.list(),
};

export function peopleQuery(mailboxId: MailboxId) {
    return { key: USER_KEYS.people(mailboxId), query: () => userApi.people(mailboxId) };
}

// Users and mailboxes each show counts of the other, so a change to either refreshes both.
export function useInvalidateAccess() {
    const cache = useQueryCache();
    return async () => {
        await cache.invalidateQueries({ key: USER_KEYS.root });
        await cache.invalidateQueries({ key: MAILBOX_KEYS.root });
    };
}
