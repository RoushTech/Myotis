import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        name: "Mailboxes",
        component: () => import("@v/MailboxesView.vue"),
    },
    {
        path: "/mailboxes/:mailboxId/:messageId?",
        name: "Inbox",
        component: () => import("@v/InboxView.vue"),
        props: (route) => ({
            mailboxId: route.params["mailboxId"],
            messageId: route.params["messageId"] || undefined,
        }),
    },
    {
        path: "/tokens",
        name: "Tokens",
        component: () => import("@v/TokensView.vue"),
    },
    {
        path: "/admin/mailboxes",
        name: "AdminMailboxes",
        component: () => import("@v/admin/AdminMailboxesView.vue"),
        meta: { admin: true },
    },
    {
        path: "/admin/mailboxes/:mailboxId",
        name: "AdminMailbox",
        component: () => import("@v/admin/AdminMailboxView.vue"),
        props: true,
        meta: { admin: true },
    },
    {
        path: "/admin/users",
        name: "AdminUsers",
        component: () => import("@v/admin/AdminUsersView.vue"),
        meta: { admin: true },
    },
    {
        path: "/admin/limits",
        name: "AdminLimits",
        component: () => import("@v/admin/AdminLimitsView.vue"),
        meta: { admin: true },
    },
    {
        path: "/admin/database",
        name: "AdminDatabase",
        component: () => import("@v/admin/AdminDatabaseView.vue"),
        meta: { admin: true },
    },
    {
        path: "/login",
        name: "Login",
        component: () => import("@v/auth/LoginView.vue"),
        meta: { guest: true },
    },
    {
        path: "/setup",
        name: "Setup",
        component: () => import("@v/auth/SetupView.vue"),
        meta: { guest: true },
    },
    { path: "/:pathMatch(.*)*", redirect: { name: "Mailboxes" } },
];

export default createRouter({
    history: createWebHistory(),
    routes,
});
