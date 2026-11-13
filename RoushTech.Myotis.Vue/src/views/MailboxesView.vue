<template>
    <v-container class="page">
        <div class="page-head">
            <div>
                <h1 class="page-title">Mailboxes</h1>
                <p class="page-subtitle">
                    {{
                        auth.isAdministrator
                            ? "Every mailbox in Myotis."
                            : "Mailboxes shared with you."
                    }}
                </p>
            </div>
        </div>
        <MailboxList />
    </v-container>
</template>

<script setup lang="ts">
import { useAuthStore } from "@/store/auth";
import MailboxList from "@c/mailboxes/MailboxList.vue";

const auth = useAuthStore();
</script>
