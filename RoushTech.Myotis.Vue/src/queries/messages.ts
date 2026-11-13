import { useQueryCache } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import { messageApi, type MessageId } from "@/api/messages";
import { MAILBOX_KEYS } from "@/queries/mailboxes";

export const MESSAGE_KEYS = {
    mailbox: (mailboxId: MailboxId) => [...MAILBOX_KEYS.detail(mailboxId), "messages"] as const,
    list: (mailboxId: MailboxId, search: string) =>
        [...MESSAGE_KEYS.mailbox(mailboxId), "list", search] as const,
    detail: (mailboxId: MailboxId, id: MessageId) =>
        [...MESSAGE_KEYS.mailbox(mailboxId), id] as const,
    raw: (mailboxId: MailboxId, id: MessageId) =>
        [...MESSAGE_KEYS.mailbox(mailboxId), id, "raw"] as const,
};

export function messagesQuery(mailboxId: MailboxId, search: string) {
    return {
        key: MESSAGE_KEYS.list(mailboxId, search),
        query: () => messageApi.list(mailboxId, search),
    };
}

export function messageQuery(mailboxId: MailboxId, id: MessageId) {
    return { key: MESSAGE_KEYS.detail(mailboxId, id), query: () => messageApi.get(mailboxId, id) };
}

export function rawQuery(mailboxId: MailboxId, id: MessageId) {
    return { key: MESSAGE_KEYS.raw(mailboxId, id), query: () => messageApi.raw(mailboxId, id) };
}

export function useInvalidateMessages() {
    const cache = useQueryCache();
    return async (mailboxId: MailboxId) => {
        await cache.invalidateQueries({ key: MESSAGE_KEYS.mailbox(mailboxId) });
        await cache.invalidateQueries({ key: MAILBOX_KEYS.root, exact: true });
    };
}
