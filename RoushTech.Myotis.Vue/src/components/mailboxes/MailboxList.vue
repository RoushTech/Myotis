<template>
    <ErrorAlert v-if="error" what="mailboxes" :message="errorMessage(error)" @retry="refetch()" />
    <v-skeleton-loader v-else-if="isLoading" type="table" />
    <NoMailboxes v-else-if="!mailboxes?.length" :admin="auth.isAdministrator" />
    <template v-else>
        <div class="toolbar">
            <v-text-field
                v-model="search"
                class="mailbox-search"
                placeholder="Find a mailbox"
                prepend-inner-icon="mdi-magnify"
                hide-details
                clearable
            />
            <v-btn-toggle v-model="filter" mandatory density="comfortable" variant="outlined">
                <v-btn value="all">All {{ mailboxes.length }}</v-btn>
                <v-btn value="unread">Unread {{ unread.length }}</v-btn>
            </v-btn-toggle>
        </div>
        <v-card border flat>
            <v-table v-if="filtered.length" hover>
                <thead>
                    <tr>
                        <th>Mailbox</th>
                        <th class="text-end">Messages</th>
                        <th>Last mail</th>
                    </tr>
                </thead>
                <tbody>
                    <tr
                        v-for="mailbox in filtered"
                        :key="mailbox.id"
                        class="clickable-row"
                        @click="open(mailbox.id)"
                    >
                        <td>
                            <span class="font-weight-medium">{{ mailbox.name }}</span>
                            <v-chip
                                v-if="mailbox.unreadCount"
                                color="primary"
                                size="x-small"
                                class="ml-2"
                            >
                                {{ mailbox.unreadCount }} new
                            </v-chip>
                        </td>
                        <td class="text-end">{{ mailbox.messageCount }}</td>
                        <td class="text-medium-emphasis">
                            {{
                                mailbox.lastReceivedAt ? listTime(mailbox.lastReceivedAt) : "Never"
                            }}
                        </td>
                    </tr>
                </tbody>
            </v-table>
            <div v-else class="empty-state">
                <v-icon icon="mdi-magnify-close" size="44" />
                <p class="empty-state__text">
                    {{
                        filter === "unread" && !query
                            ? "No unread mail in any mailbox."
                            : `No mailbox matches "${query}".`
                    }}
                </p>
            </div>
        </v-card>
    </template>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { useQuery } from "@pinia/colada";
import type { MailboxId } from "@/api/mailboxes";
import { errorMessage } from "@/api/errors";
import { listTime } from "@/lib/dates";
import { mailboxesQuery } from "@/queries/mailboxes";
import { useAuthStore } from "@/store/auth";
import ErrorAlert from "@c/ErrorAlert.vue";
import NoMailboxes from "@c/mailboxes/NoMailboxes.vue";

const auth = useAuthStore();
const { data: mailboxes, error, isLoading, refetch } = useQuery(mailboxesQuery);
const router = useRouter();
const search = ref<string | null>("");
const filter = ref<"all" | "unread">("all");
const query = computed(() => (search.value ?? "").trim());

const unread = computed(() => (mailboxes.value ?? []).filter((m) => m.unreadCount > 0));
const filtered = computed(() =>
    (filter.value === "unread" ? unread.value : (mailboxes.value ?? [])).filter((m) =>
        m.name.toLowerCase().includes(query.value.toLowerCase())
    )
);

function open(mailboxId: MailboxId) {
    void router.push({ name: "Inbox", params: { mailboxId } });
}
</script>
