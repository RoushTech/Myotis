<template>
    <v-container class="page">
        <div class="page-head">
            <div>
                <h1 class="page-title">Mailboxes</h1>
                <p class="page-subtitle">
                    Each mailbox has its own SMTP credentials. Mail sent with a credential lands in
                    its mailbox, whoever it's addressed to.
                </p>
            </div>
            <NewMailboxDialog />
        </div>
        <AdminMailboxTable />
    </v-container>
</template>

<script setup lang="ts">
import AdminMailboxTable from "@c/admin/AdminMailboxTable.vue";
import NewMailboxDialog from "@c/admin/NewMailboxDialog.vue";
</script>
