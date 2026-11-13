import axios from "axios";

export type MailboxId = string & { __brand: "MailboxId" };
export type CredentialId = string & { __brand: "CredentialId" };

export type Mailbox = {
    id: MailboxId;
    name: string;
    createdAt: string;
    credentialCount: number;
};

export type Credential = {
    id: CredentialId;
    username: string;
    createdAt: string;
    lastUsedAt: string | null;
};

export type CreatedCredential = { credential: Credential; password: string };

export const mailboxApi = {
    async list(): Promise<Mailbox[]> {
        return (await axios.get<Mailbox[]>("/api/v0/mailboxes")).data;
    },

    async get(id: MailboxId): Promise<Mailbox> {
        return (await axios.get<Mailbox>(`/api/v0/mailboxes/${id}`)).data;
    },

    async create(name: string): Promise<Mailbox> {
        return (await axios.post<Mailbox>("/api/v0/mailboxes", { name })).data;
    },

    async remove(id: MailboxId): Promise<void> {
        await axios.delete(`/api/v0/mailboxes/${id}`);
    },

    async credentials(id: MailboxId): Promise<Credential[]> {
        return (await axios.get<Credential[]>(`/api/v0/mailboxes/${id}/credentials`)).data;
    },

    async createCredential(id: MailboxId, username: string): Promise<CreatedCredential> {
        return (
            await axios.post<CreatedCredential>(`/api/v0/mailboxes/${id}/credentials`, {
                username,
            })
        ).data;
    },

    async revokeCredential(id: MailboxId, credentialId: CredentialId): Promise<void> {
        await axios.delete(`/api/v0/mailboxes/${id}/credentials/${credentialId}`);
    },
};
