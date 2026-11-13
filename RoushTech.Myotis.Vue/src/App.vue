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
                    >
                        <template v-if="totalUnread" #append>
                            <v-badge :content="totalUnread" color="primary" inline />
                        </template>
                    </v-list-item>
                    <v-list-subheader>Recent</v-list-subheader>
                    <v-list-item
                        v-for="mailbox in recent"
                        :key="mailbox.id"
                        prepend-icon="mdi-email-outline"
                        :title="mailbox.name"
                        :to="{ name: 'Inbox', params: { mailboxId: mailbox.id } }"
                        color="primary"
                    >
                        <template v-if="mailbox.unreadCount" #append>
                            <v-badge :content="mailbox.unreadCount" color="primary" inline />
                        </template>
                    </v-list-item>
                    <div v-if="!recent.length" class="app-nav__hint">
                        Mailboxes you open show here.
                    </div>
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
                        <v-list-item
                            prepend-icon="mdi-tune-variant"
                            title="Limits"
                            :to="{ name: 'AdminLimits' }"
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
        <v-snackbar v-model="announced" timeout="4000" location="bottom">
            {{ announcement }}
        </v-snackbar>
    </v-app>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { HubConnection } from "@microsoft/signalr";
import { useRouter } from "vue-router";
import { useDisplay } from "vuetify";
import { useQuery, useQueryCache } from "@pinia/colada";
import { connectLive } from "./api/live";
import type { MessageSummary } from "./api/messages";
import { recentMailboxes } from "./lib/recent";
import { aboutQuery } from "./queries/about";
import { mailboxesQuery } from "./queries/mailboxes";
import { useInvalidateMessages } from "./queries/messages";
import { useAuthStore } from "./store/auth";
import AppBrand from "@c/AppBrand.vue";

const auth = useAuthStore();
const router = useRouter();
const cache = useQueryCache();
const { mobile } = useDisplay();
const drawer = ref(!mobile.value);
const { data: about } = useQuery({ ...aboutQuery, enabled: () => auth.isAuthenticated });

const { data: mailboxes } = useQuery({ ...mailboxesQuery, enabled: () => auth.isAuthenticated });
const invalidateMessages = useInvalidateMessages();
const announced = ref(false);
const announcement = ref("");
let live: HubConnection | null = null;

const totalUnread = computed(() => (mailboxes.value ?? []).reduce((n, m) => n + m.unreadCount, 0));
const recent = computed(() =>
    recentMailboxes.value
        .map((id) => mailboxes.value?.find((m) => m.id === id))
        .filter((m) => m !== undefined)
);

watch(mobile, (isMobile) => (drawer.value = !isMobile));

watch(
    () => auth.isAuthenticated,
    (signedIn) => {
        if (signedIn && !live) {
            live = connectLive(onMessage);
        } else if (!signedIn && live) {
            void live.stop();
            live = null;
        }
    },
    { immediate: true }
);

function onMessage(message: MessageSummary) {
    void invalidateMessages(message.mailboxId);
    const name = mailboxes.value?.find((m) => m.id === message.mailboxId)?.name;
    announcement.value = name ? `New message in ${name}` : "New message";
    announced.value = true;
}

async function logout() {
    await auth.logout();
    cache.getEntries().forEach((entry) => cache.remove(entry));
    await router.push({ name: "Login" });
}
</script>
