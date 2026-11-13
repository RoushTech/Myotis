import axios from "axios";

export type SmtpConnection = { host: string; ports: { port: number; security: string }[] };

export const smtpApi = {
    async connection(): Promise<SmtpConnection> {
        return (await axios.get<SmtpConnection>("/api/v0/smtp")).data;
    },
};
