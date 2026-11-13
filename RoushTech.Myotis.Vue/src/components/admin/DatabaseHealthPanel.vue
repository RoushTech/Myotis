<template>
    <div class="page-head">
        <div>
            <h1 class="page-title">Database health</h1>
            <p class="page-subtitle">One SQLite file holds every message.</p>
        </div>
        <v-btn
            variant="outlined"
            prepend-icon="mdi-refresh"
            :loading="isLoading"
            @click="refetch()"
        >
            Refresh
        </v-btn>
    </div>
    <ErrorAlert
        v-if="error"
        what="database health"
        :message="errorMessage(error)"
        @retry="refetch()"
    />
    <v-skeleton-loader v-else-if="!health" type="card@3" />
    <div v-else class="stack">
        <div class="stats">
            <v-card border flat class="stat">
                <div class="stat__label">Database file</div>
                <div class="stat__value">{{ size(health.fileBytes) }}</div>
                <div class="stat__sub text-mono">{{ health.fileName }}</div>
            </v-card>
            <v-card border flat class="stat">
                <div class="stat__label">Write-ahead log</div>
                <div class="stat__value">{{ size(health.writeAheadLogBytes) }}</div>
                <div class="stat__sub">Folded in automatically</div>
            </v-card>
            <v-card border flat class="stat">
                <div class="stat__label">Reclaimable</div>
                <div class="stat__value">{{ size(health.reclaimableBytes) }}</div>
                <div class="stat__sub">Space freed by deletes</div>
            </v-card>
            <v-card border flat class="stat">
                <div class="stat__label">Messages</div>
                <div class="stat__value">{{ health.messageCount.toLocaleString() }}</div>
                <div class="stat__sub">{{ health.mailboxes.length }} mailboxes</div>
            </v-card>
        </div>
        <v-card border flat>
            <v-card-item><v-card-title>Housekeeping</v-card-title></v-card-item>
            <v-card-text>
                <dl class="key-values">
                    <dt>Last retention run</dt>
                    <dd>
                        <template v-if="!health.lastRetention">Not run yet</template>
                        <template v-else>
                            {{ relative(health.lastRetention.startedAt) }}, deleted
                            {{ health.lastRetention.deletedCount }}
                            {{ health.lastRetention.deletedCount === 1 ? "message" : "messages" }}
                            <v-chip
                                :color="health.lastRetention.error ? 'error' : 'success'"
                                size="small"
                                variant="tonal"
                                class="ml-2"
                            >
                                {{ health.lastRetention.error ? "Failed" : "Succeeded" }}
                            </v-chip>
                            <div v-if="health.lastRetention.error" class="text-error mt-1">
                                {{ health.lastRetention.error }}
                            </div>
                        </template>
                    </dd>
                    <template v-if="health.lastRetention">
                        <dt>Next retention run</dt>
                        <dd>{{ relative(health.lastRetention.nextRunAt) }}</dd>
                    </template>
                    <dt>Integrity check</dt>
                    <dd class="integrity">
                        <span v-if="!integrity" class="text-medium-emphasis"
                            >Not run this visit</span
                        >
                        <v-chip
                            v-else-if="integrity.ok"
                            color="success"
                            size="small"
                            variant="tonal"
                        >
                            No problems found
                        </v-chip>
                        <span v-else class="text-error">{{ integrity.problems.join("; ") }}</span>
                        <v-btn variant="outlined" size="small" :loading="checking" @click="check">
                            Run integrity check
                        </v-btn>
                    </dd>
                </dl>
            </v-card-text>
        </v-card>
        <v-card border flat>
            <v-card-item><v-card-title>By mailbox</v-card-title></v-card-item>
            <v-table v-if="health.mailboxes.length">
                <thead>
                    <tr>
                        <th>Mailbox</th>
                        <th class="text-end">Messages</th>
                        <th class="text-end">Size</th>
                        <th>Share</th>
                        <th>Oldest message</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="mailbox in health.mailboxes" :key="mailbox.id">
                        <td>{{ mailbox.name }}</td>
                        <td class="text-end">{{ mailbox.messageCount.toLocaleString() }}</td>
                        <td class="text-end">{{ size(mailbox.bytes) }}</td>
                        <td class="share-cell">
                            <div class="share-bar">
                                <i :style="{ width: `${share(mailbox.bytes)}%` }" />
                            </div>
                        </td>
                        <td class="text-medium-emphasis">
                            {{
                                mailbox.oldestReceivedAt
                                    ? shortDate(mailbox.oldestReceivedAt)
                                    : "None"
                            }}
                        </td>
                    </tr>
                </tbody>
            </v-table>
            <div v-else class="empty-state">
                <v-icon icon="mdi-database-outline" size="44" />
                <p class="empty-state__text">No mail stored yet.</p>
            </div>
        </v-card>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useQuery } from "@pinia/colada";
import { databaseApi, type IntegrityResult } from "@/api/database";
import { errorMessage } from "@/api/errors";
import { shortDate } from "@/lib/dates";
import { relative } from "@/lib/relative";
import { size } from "@/lib/sizes";
import { databaseHealthQuery } from "@/queries/database";
import ErrorAlert from "@c/ErrorAlert.vue";

const { data: health, error, isLoading, refetch } = useQuery(databaseHealthQuery);
const integrity = ref<IntegrityResult | null>(null);
const checking = ref(false);
const total = computed(() => (health.value?.mailboxes ?? []).reduce((n, m) => n + m.bytes, 0));

function share(bytes: number): number {
    return total.value ? (bytes / total.value) * 100 : 0;
}

async function check() {
    checking.value = true;
    try {
        integrity.value = await databaseApi.check();
    } finally {
        checking.value = false;
    }
}
</script>
