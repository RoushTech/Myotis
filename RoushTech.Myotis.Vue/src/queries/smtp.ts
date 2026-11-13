import { smtpApi } from "@/api/smtp";

export const smtpConnectionQuery = {
    key: ["smtp"],
    query: () => smtpApi.connection(),
    staleTime: 10 * 60_000,
};
