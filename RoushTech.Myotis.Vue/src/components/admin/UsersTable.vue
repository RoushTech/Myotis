<template>
    <div class="page-head">
        <div>
            <h1 class="page-title">Users</h1>
            <p class="page-subtitle">
                You add people here and choose which mailboxes they can read.
            </p>
        </div>
        <v-btn color="primary" prepend-icon="mdi-account-plus-outline" @click="edit(null)">
            Add user
        </v-btn>
    </div>
    <ErrorAlert v-if="error" what="users" :message="errorMessage(error)" @retry="refetch()" />
    <v-skeleton-loader v-else-if="isLoading" type="table" />
    <template v-else>
        <v-card border flat>
            <v-table>
                <thead>
                    <tr>
                        <th>Email</th>
                        <th>Mailboxes</th>
                        <th>Last sign-in</th>
                        <th />
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="user in users" :key="user.id">
                        <td>
                            {{ user.email }}
                            <span v-if="user.id === auth.me?.id" class="text-medium-emphasis">
                                (you)
                            </span>
                        </td>
                        <td>
                            <v-chip
                                v-if="user.isAdministrator"
                                color="primary"
                                size="small"
                                variant="tonal"
                            >
                                Administrator, sees all
                            </v-chip>
                            <span v-else-if="!user.mailboxIds.length" class="text-medium-emphasis">
                                None yet
                            </span>
                            <div v-else class="chips">
                                <v-chip
                                    v-for="name in names(user).slice(0, 3)"
                                    :key="name"
                                    size="small"
                                >
                                    {{ name }}
                                </v-chip>
                                <v-chip
                                    v-if="names(user).length > 3"
                                    size="small"
                                    color="primary"
                                    variant="tonal"
                                    :title="names(user).slice(3).join(', ')"
                                >
                                    +{{ names(user).length - 3 }} more
                                </v-chip>
                            </div>
                        </td>
                        <td class="text-medium-emphasis">{{ lastSignIn(user.lastSignInAt) }}</td>
                        <td class="text-end">
                            <v-btn variant="text" size="small" color="primary" @click="edit(user)">
                                Edit
                            </v-btn>
                        </td>
                    </tr>
                </tbody>
            </v-table>
        </v-card>
        <v-alert v-if="users?.length === 1" type="info" variant="tonal" class="mt-4">
            You're the only user. Add people, then share mailboxes with them.
        </v-alert>
    </template>
    <UserDialog :open="dialogOpen" :user="editing" @close="dialogOpen = false" />
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useQuery } from "@pinia/colada";
import type { User } from "@/api/users";
import { errorMessage } from "@/api/errors";
import { lastSignIn } from "@/lib/dates";
import { mailboxesQuery } from "@/queries/mailboxes";
import { usersQuery } from "@/queries/users";
import { useAuthStore } from "@/store/auth";
import ErrorAlert from "@c/ErrorAlert.vue";
import UserDialog from "@c/admin/UserDialog.vue";

const auth = useAuthStore();
const { data: users, error, isLoading, refetch } = useQuery(usersQuery);
const { data: mailboxes } = useQuery(mailboxesQuery);
const dialogOpen = ref(false);
const editing = ref<User | null>(null);

const mailboxNames = computed(() => new Map((mailboxes.value ?? []).map((m) => [m.id, m.name])));

function names(user: User): string[] {
    return user.mailboxIds
        .map((id) => mailboxNames.value.get(id))
        .filter((name): name is string => name !== undefined)
        .sort((a, b) => a.localeCompare(b));
}

function edit(user: User | null) {
    editing.value = user;
    dialogOpen.value = true;
}
</script>
