import axios from "axios";
import type { MailboxId } from "@/api/mailboxes";

export type DatabaseHealth = {
    fileName: string;
    fileBytes: number;
    writeAheadLogBytes: number;
    reclaimableBytes: number;
    messageCount: number;
    mailboxes: {
        id: MailboxId;
        name: string;
        messageCount: number;
        bytes: number;
        oldestReceivedAt: string | null;
    }[];
    lastRetention: {
        startedAt: string;
        finishedAt: string | null;
        deletedCount: number;
        error: string | null;
        nextRunAt: string;
    } | null;
};

export type IntegrityResult = { ok: boolean; problems: string[]; checkedAt: string };

export const databaseApi = {
    async health(): Promise<DatabaseHealth> {
        return (await axios.get<DatabaseHealth>("/api/v0/database")).data;
    },

    async check(): Promise<IntegrityResult> {
        return (await axios.post<IntegrityResult>("/api/v0/database/check")).data;
    },
};
