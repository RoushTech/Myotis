import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import type { MessageSummary } from "@/api/messages";

export function connectLive(onMessage: (message: MessageSummary) => void): HubConnection {
    const connection = new HubConnectionBuilder()
        .withUrl("/hubs/mail")
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();
    connection.on("MessageReceived", onMessage);
    void connection.start();
    return connection;
}
