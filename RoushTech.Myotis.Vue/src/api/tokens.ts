import axios from "axios";

export type TokenId = string & { __brand: "TokenId" };

export type Token = {
    id: TokenId;
    name: string;
    prefix: string;
    createdAt: string;
    expiresAt: string | null;
    lastUsedAt: string | null;
};

export const tokenApi = {
    async list(): Promise<Token[]> {
        return (await axios.get<Token[]>("/api/v0/tokens")).data;
    },

    async create(name: string): Promise<{ token: Token; plainText: string }> {
        return (await axios.post<{ token: Token; plainText: string }>("/api/v0/tokens", { name }))
            .data;
    },

    async revoke(id: TokenId): Promise<void> {
        await axios.delete(`/api/v0/tokens/${id}`);
    },
};
