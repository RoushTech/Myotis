import axios from "axios";

export type UserId = string & { __brand: "UserId" };

export type Me = {
    id: UserId;
    email: string;
    isAdministrator: boolean;
};

export const authApi = {
    async state(): Promise<{ setupRequired: boolean }> {
        const { data } = await axios.get<{ setupRequired: boolean }>("/api/v0/auth/state");
        return data;
    },

    async me(): Promise<Me> {
        const { data } = await axios.get<Me>("/api/v0/auth/me");
        return data;
    },

    async setup(email: string, password: string): Promise<Me> {
        const { data } = await axios.post<Me>("/api/v0/auth/setup", { email, password });
        return data;
    },

    async login(email: string, password: string, rememberMe: boolean): Promise<Me> {
        const { data } = await axios.post<Me>("/api/v0/auth/login", {
            email,
            password,
            rememberMe,
        });
        return data;
    },

    async logout(): Promise<void> {
        await axios.post("/api/v0/auth/logout");
    },
};
