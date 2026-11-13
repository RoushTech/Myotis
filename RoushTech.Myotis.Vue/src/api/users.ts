import axios from "axios";
import type { UserId } from "@/api/auth";
import type { MailboxId } from "@/api/mailboxes";

export type User = {
    id: UserId;
    email: string;
    isAdministrator: boolean;
    lastSignInAt: string | null;
    mailboxIds: MailboxId[];
};

export type NewUser = {
    email: string;
    password: string;
    isAdministrator: boolean;
    mailboxIds: MailboxId[];
};

export type UserChanges = {
    isAdministrator?: boolean;
    mailboxIds?: MailboxId[];
    password?: string;
};

export type Person = { userId: UserId; email: string; isAdministrator: boolean };

export const userApi = {
    async list(): Promise<User[]> {
        return (await axios.get<User[]>("/api/v0/users")).data;
    },

    async create(user: NewUser): Promise<User> {
        return (await axios.post<User>("/api/v0/users", user)).data;
    },

    async update(id: UserId, changes: UserChanges): Promise<User> {
        return (await axios.patch<User>(`/api/v0/users/${id}`, changes)).data;
    },

    async remove(id: UserId): Promise<void> {
        await axios.delete(`/api/v0/users/${id}`);
    },

    async people(mailboxId: MailboxId): Promise<Person[]> {
        return (await axios.get<Person[]>(`/api/v0/mailboxes/${mailboxId}/people`)).data;
    },

    async addPerson(mailboxId: MailboxId, userId: UserId): Promise<Person[]> {
        return (await axios.post<Person[]>(`/api/v0/mailboxes/${mailboxId}/people`, { userId }))
            .data;
    },

    async removePerson(mailboxId: MailboxId, userId: UserId): Promise<void> {
        await axios.delete(`/api/v0/mailboxes/${mailboxId}/people/${userId}`);
    },
};
