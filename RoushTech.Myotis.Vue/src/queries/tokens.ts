import { useQueryCache } from "@pinia/colada";
import { tokenApi } from "@/api/tokens";

export const TOKEN_KEYS = {
    root: ["tokens"] as const,
};

export const tokensQuery = {
    key: TOKEN_KEYS.root,
    query: () => tokenApi.list(),
};

export function useInvalidateTokens() {
    const cache = useQueryCache();
    return () => cache.invalidateQueries({ key: TOKEN_KEYS.root });
}
