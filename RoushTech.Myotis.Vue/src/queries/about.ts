import { aboutApi } from "@/api/about";

export const aboutQuery = {
    key: ["about"],
    query: () => aboutApi.about(),
    staleTime: 10 * 60_000,
};
