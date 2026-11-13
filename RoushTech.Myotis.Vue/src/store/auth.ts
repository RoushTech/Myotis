import { defineStore } from "pinia";
import { authApi, type Me } from "@/api/auth";
import { isUnauthorized } from "@/api/errors";

export const useAuthStore = defineStore("auth", {
    state: () => ({
        me: null as Me | null,
        setupRequired: false,
        route: null as string | null,
    }),
    getters: {
        isAuthenticated: (state) => state.me !== null,
        isAdministrator: (state) => state.me?.isAdministrator ?? false,
    },
    actions: {
        async check(): Promise<boolean> {
            this.setupRequired = (await authApi.state()).setupRequired;
            if (this.setupRequired) {
                this.me = null;
                return false;
            }
            try {
                this.me = await authApi.me();
            } catch (error) {
                if (!isUnauthorized(error)) {
                    throw error;
                }
                this.me = null;
            }
            return this.me !== null;
        },

        signedIn(me: Me) {
            this.me = me;
            this.setupRequired = false;
        },

        async logout(): Promise<void> {
            await authApi.logout();
            this.me = null;
        },
    },
});
