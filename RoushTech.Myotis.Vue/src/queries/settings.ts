import { useQueryCache } from "@pinia/colada";
import { settingsApi } from "@/api/settings";

export const SETTING_KEYS = {
    root: ["settings"] as const,
};

export const settingsQuery = {
    key: SETTING_KEYS.root,
    query: () => settingsApi.list(),
};

export function useInvalidateSettings() {
    const cache = useQueryCache();
    return () => cache.invalidateQueries({ key: SETTING_KEYS.root });
}
