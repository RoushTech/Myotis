<template>
    <v-app>
        <template v-if="auth.isAuthenticated">
            <v-app-bar v-if="mobile" density="comfortable" flat border>
                <v-app-bar-nav-icon aria-label="Menu" @click="drawer = !drawer" />
                <v-app-bar-title><AppBrand /></v-app-bar-title>
            </v-app-bar>
            <v-navigation-drawer
                v-model="drawer"
                class="app-nav"
                :permanent="!mobile"
                :temporary="mobile"
            >
                <router-link v-if="!mobile" to="/" class="app-nav__brand">
                    <AppBrand />
                </router-link>
                <v-list nav density="compact">
                    <v-list-item
                        prepend-icon="mdi-inbox-multiple-outline"
                        title="All mailboxes"
                        :to="{ name: 'Mailboxes' }"
                        exact
                        color="primary"
                    />
                    <v-list-subheader>Recent</v-list-subheader>
                    <div class="app-nav__hint">Mailboxes you open show here.</div>
                    <template v-if="auth.isAdministrator">
                        <v-list-subheader>Administration</v-list-subheader>
                        <v-list-item
                            prepend-icon="mdi-inbox-full-outline"
                            title="Mailboxes"
                            :to="{ name: 'AdminMailboxes' }"
                            color="primary"
                        />
                        <v-list-item
                            prepend-icon="mdi-account-multiple-outline"
                            title="Users"
                            :to="{ name: 'AdminUsers' }"
                            color="primary"
                        />
                    </template>
                </v-list>
                <template #append>
                    <v-divider />
                    <v-list nav density="compact">
                        <v-list-item
                            prepend-icon="mdi-logout"
                            title="Sign out"
                            :subtitle="auth.me?.email ?? ''"
                            @click="logout"
                        />
                    </v-list>
                    <div v-if="about" class="app-nav__version">Version {{ about.version }}</div>
                </template>
            </v-navigation-drawer>
        </template>
        <v-main>
            <router-view />
        </v-main>
    </v-app>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import { useRouter } from "vue-router";
import { useDisplay } from "vuetify";
import { useQuery, useQueryCache } from "@pinia/colada";
import { aboutQuery } from "./queries/about";
import { useAuthStore } from "./store/auth";
import AppBrand from "@c/AppBrand.vue";

const auth = useAuthStore();
const router = useRouter();
const cache = useQueryCache();
const { mobile } = useDisplay();
const drawer = ref(!mobile.value);
const { data: about } = useQuery({ ...aboutQuery, enabled: () => auth.isAuthenticated });

watch(mobile, (isMobile) => (drawer.value = !isMobile));

async function logout() {
    await auth.logout();
    cache.getEntries().forEach((entry) => cache.remove(entry));
    await router.push({ name: "Login" });
}
</script>
