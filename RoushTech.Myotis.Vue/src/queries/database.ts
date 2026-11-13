import { databaseApi } from "@/api/database";

export const databaseHealthQuery = {
    key: ["database"],
    query: () => databaseApi.health(),
    staleTime: 0,
};
