namespace RoushTech.Myotis.Website.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

// Clients only listen. The server decides who hears about each message, see SignalRMailNotifier.
[Authorize]
public class MailHub : Hub;
