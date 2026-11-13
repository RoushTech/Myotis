import axios from "axios";

export type Setting = {
    key: string;
    group: string;
    name: string;
    unit: string;
    default: number;
    min: number;
    max: number;
    value: number;
};

export type SettingChange = { key: string; value?: string; clear?: boolean };

export const settingsApi = {
    async list(): Promise<Setting[]> {
        return (await axios.get<Setting[]>("/api/v0/settings")).data;
    },

    async update(changes: SettingChange[]): Promise<Setting[]> {
        return (await axios.patch<Setting[]>("/api/v0/settings", { changes })).data;
    },
};
