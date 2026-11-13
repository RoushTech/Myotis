import axios from "axios";
import type { MailboxId } from "@/api/mailboxes";

export type MessageId = string & { __brand: "MessageId" };

export type MessageSummary = {
    id: MessageId;
    mailboxId: MailboxId;
    receivedAt: string;
    subject: string;
    from: string;
    to: string;
    cc: string;
    size: number;
    attachmentCount: number;
    isRead: boolean;
};

export type Attachment = { index: number; fileName: string; contentType: string; size: number };

export type MessageDetail = {
    summary: MessageSummary;
    envelopeFrom: string;
    envelopeTo: string[];
    sentAt: string | null;
    headers: { key: string; value: string }[];
    text: string | null;
    hasHtml: boolean;
    remoteImageCount: number;
    attachments: Attachment[];
};

function base(mailboxId: MailboxId, id: MessageId): string {
    return `/api/v0/mailboxes/${mailboxId}/messages/${id}`;
}

export const messageApi = {
    async list(mailboxId: MailboxId, search: string): Promise<MessageSummary[]> {
        return (
            await axios.get<MessageSummary[]>(`/api/v0/mailboxes/${mailboxId}/messages`, {
                params: search ? { search } : {},
            })
        ).data;
    },

    async get(mailboxId: MailboxId, id: MessageId): Promise<MessageDetail> {
        return (await axios.get<MessageDetail>(base(mailboxId, id))).data;
    },

    async markRead(mailboxId: MailboxId, id: MessageId): Promise<void> {
        await axios.patch(base(mailboxId, id), { isRead: true });
    },

    async raw(mailboxId: MailboxId, id: MessageId): Promise<string> {
        return (await axios.get<string>(`${base(mailboxId, id)}/raw`, { responseType: "text" }))
            .data;
    },

    htmlUrl(mailboxId: MailboxId, id: MessageId, remoteImages: boolean): string {
        return `${base(mailboxId, id)}/html${remoteImages ? "?remoteImages=true" : ""}`;
    },

    emlUrl(mailboxId: MailboxId, id: MessageId): string {
        return `${base(mailboxId, id)}/raw?download=true`;
    },

    attachmentUrl(mailboxId: MailboxId, id: MessageId, index: number): string {
        return `${base(mailboxId, id)}/attachments/${index}`;
    },
};
