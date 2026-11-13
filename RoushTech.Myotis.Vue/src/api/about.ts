import axios from "axios";

export type About = { version: string };

export const aboutApi = {
    async about(): Promise<About> {
        return (await axios.get<About>("/api/v0/about")).data;
    },
};
