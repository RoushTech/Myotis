# Myotis

Myotis catches the email your test and staging environments send, so none of it reaches a real inbox. Apps send to
one SMTP endpoint with a per-mailbox credential, the message lands in that mailbox, and the people you've given that
mailbox to read it in the browser. Nothing is ever relayed or delivered.

Named for the mouse-eared bats: they catch things in flight, and Myotis catches mail in transit.

## Why another mail catcher

[smtp4dev](https://github.com/rnwood/smtp4dev) and [Mailpit](https://github.com/axllent/mailpit) are excellent, and
for one developer or one team they're probably all you need. Myotis is for when several teams, environments or
clients share one catcher and each should only see its own mail:

- **Mailboxes are the unit of access.** Each person is added to the mailboxes they may read. They don't see anyone
  else's, and an ID they can't see reads exactly like one that doesn't exist.
- **The credential decides the mailbox, never the recipient.** Every mailbox has its own SMTP credentials. A message
  addressed to anyone at all lands in the mailbox whose credential sent it, so a typo in a recipient can't put one
  client's mail in front of another.
- **It's built to sit on a public port.** Authentication is required before `MAIL FROM`, passwords are stored hashed,
  repeated failed sign-ins block the address, each credential has a sending rate, and message size is capped while
  the message arrives.
- **It's built for tests and agents too.** Personal access tokens drive a stable HTTP API and an MCP endpoint, so a
  test suite or an AI agent can find the password reset email it just triggered and follow its link.
- **One process, one file.** The web app and the SMTP listener run together, and everything, raw messages included,
  lives in a single SQLite file.

What Myotis doesn't do: deliver or release caught mail, check spam scores or client rendering, or serve IMAP and POP3.

## Features

- Mailboxes with any number of SMTP credentials, each password shown once when it's created.
- Administrators create users and add them to mailboxes. There's no sign-up, and Myotis never sends email itself.
- An inbox with search, live updates, read state shared by everyone on the mailbox, and a viewer for HTML, text,
  headers, raw source and attachments.
- The full SMTP envelope is kept with each message, so Bcc recipients are visible.
- Message HTML is treated as hostile. It's served from its own endpoint under a strict Content Security Policy and
  shown in a sandboxed frame with no scripts. Remote images stay blocked until someone asks for them, one message at
  a time. Attachments always download, never open in the browser.
- Retention by count and by age, plus deleting one message or emptying a mailbox.
- Limits (message size, retention, sending rate, sign-in protection) that administrators change at runtime.
- A database health page with sizes, per-mailbox usage, the last retention run and an integrity check.

## Running it

Myotis ships as a container image that serves HTTP on port 80 and SMTP on whichever ports you configure. Build it from
this repository:

```bash
docker build -t myotis .
```

A minimal run with plaintext SMTP behind a TLS-terminating proxy:

```bash
docker run -d --name myotis \
  -p 8080:80 -p 2525:2525 \
  -v myotis-data:/app/data \
  -e Myotis__Smtp__PublicHost=mail.example.com \
  -e Myotis__Smtp__Listeners__0__Port=2525 \
  -e Myotis__Smtp__Listeners__0__Tls=Proxy \
  -e Myotis__Smtp__Listeners__0__PublicPort=465 \
  myotis
```

Open the web UI and **create the first administrator straight away**: until someone does, whoever reaches the setup
page first becomes the administrator. To skip the setup page, seed the administrator instead with
`Myotis__Seed__AdminEmail` and `Myotis__Seed__AdminPassword`, which only apply to an empty database.

Then create a mailbox, add a credential, and point your app's SMTP settings at Myotis with that username and password.

Run one replica only. SQLite has a single writer, and two containers must never open the same file.

## SMTP and TLS

Each entry under `Myotis:Smtp:Listeners` is a port with a TLS mode. Sign-in is only offered over TLS, so pick the mode
that matches how senders reach that port:

| `Tls` | Use it for |
|---|---|
| `Implicit` | TLS from the first byte, usually port 465. Needs a certificate. |
| `StartTls` | Plaintext until the client upgrades, usually port 587. Sign-in is only offered after the upgrade. Needs a certificate. |
| `Proxy` | Plaintext to Myotis because a proxy in front already terminated TLS. |
| `None` | Plaintext with no TLS anywhere. Sign-in only works in the `Development` environment. |

For `Implicit` and `StartTls`, point `Myotis:Smtp:Certificate:CertificatePath` and `KeyPath` at PEM files. Myotis
reloads them when they change, so renewals need no restart.

`PublicPort` is the port senders use when a proxy or container maps it to a different one. It's what the credential
dialog shows, alongside `PublicHost`.

### Behind a proxy

When a proxy such as Traefik terminates TLS, every connection reaches Myotis from the proxy's address. That would make
the failed sign-in block useless, or worse, block everyone at once. Have the proxy send a PROXY protocol **version 1**
header and list the proxy's addresses in `Myotis:Smtp:TrustedProxies`. Myotis ignores the header from anyone else,
because any client can send one.

A Traefik TCP router for implicit TLS on an `smtps` entrypoint, as Docker labels:

```yaml
- "traefik.tcp.routers.myotis-smtp.rule=HostSNI(`mail.example.com`)"
- "traefik.tcp.routers.myotis-smtp.entrypoints=smtps"
- "traefik.tcp.routers.myotis-smtp.tls=true"
- "traefik.tcp.routers.myotis-smtp.tls.certresolver=letsencrypt"
- "traefik.tcp.services.myotis-smtp.loadbalancer.server.port=2525"
- "traefik.tcp.services.myotis-smtp.loadbalancer.proxyProtocol.version=1"
```

The web side works the same way: list the proxy in `Myotis:Web:TrustedProxies` so Myotis believes its
`X-Forwarded-Proto` and marks the sign-in cookie secure. With nothing listed, forwarded headers are ignored.

## Configuration

Settings come from `appsettings.json`, environment variables (`Myotis__Smtp__PublicHost` for `Myotis:Smtp:PublicHost`)
or the command line.

| Setting | Meaning |
|---|---|
| `ConnectionStrings:Default` | The SQLite file, such as `Data Source=/app/data/myotis.db`. |
| `Myotis:Smtp:PublicHost` | The host senders connect to, shown with each new credential. |
| `Myotis:Smtp:Listeners:N:Port` / `Tls` / `PublicPort` | One SMTP listener, see above. |
| `Myotis:Smtp:Certificate:CertificatePath` / `KeyPath` | PEM certificate and key for `Implicit` and `StartTls`. |
| `Myotis:Smtp:TrustedProxies` | Comma-separated addresses or CIDR ranges whose PROXY header is believed. |
| `Myotis:Web:TrustedProxies` | Comma-separated addresses or CIDR ranges whose forwarded headers are believed. |
| `Myotis:Seed:AdminEmail` / `AdminPassword` | Creates the first administrator on an empty database. |

Limits live in the database and are changed on the Limits page:

| Limit | Default |
|---|---|
| Largest message | 25 MB |
| Messages kept per mailbox | 1,000 |
| Days a message is kept | 14 |
| Messages a minute per credential | 300 |
| Failed sign-ins before the connection closes | 3 |
| Failed sign-ins from one address before it's blocked | 10 within 10 minutes, blocked for 15 minutes |

A sender over its rate gets a temporary `451`, so well-behaved clients retry later.

## Test suites and agents

Every user can create personal access tokens on the Access tokens page. A token reads exactly the mailboxes its owner
can, and only works on `/api/v1` and `/mcp`. The browser session never works there, and a token never works anywhere
else.

`/api/v1` is a stable contract: fields may be added, never renamed or removed.

```bash
curl -H "Authorization: Bearer myotis_pat_..." https://myotis.example.com/api/v1/mailboxes
curl -H "Authorization: Bearer myotis_pat_..." \
  "https://myotis.example.com/api/v1/mailboxes/{mailbox}/messages?since=2026-11-13T09:00:00Z&search=reset"
curl -H "Authorization: Bearer myotis_pat_..." https://myotis.example.com/api/v1/mailboxes/{mailbox}/messages/{message}
curl -X DELETE -H "Authorization: Bearer myotis_pat_..." https://myotis.example.com/api/v1/mailboxes/{mailbox}/messages
```

A message comes back with its text and HTML bodies, envelope, attachments and every link it contains, ready for a test
to follow.

The MCP endpoint offers the same reach as `list_mailboxes`, `list_messages`, `read_message` and `empty_mailbox`:

```bash
claude mcp add --transport http myotis https://myotis.example.com/mcp \
  --header "Authorization: Bearer myotis_pat_..."
```

## Development

You need the .NET 10 SDK and Node 22 or later.

```bash
cp RoushTech.Myotis.Website/appsettings.example.json RoushTech.Myotis.Website/appsettings.local.json
dotnet watch --project RoushTech.Myotis.Website
```

The server runs on http://localhost:5400 and starts Vite on http://localhost:3004. SMTP listens on 2526 with plaintext
sign-in allowed, because `dotnet watch` runs in the `Development` environment. The example settings seed
`admin@roushtech.net` / `Password1` on an empty database. The SQLite file goes in `.dev/`.

Before sending a change, run `dotnet format`, `dotnet test`, and `npm run lint` in `RoushTech.Myotis.Vue`. A pre-commit
hook checks formatting and lint for you.

## License

Myotis is licensed under the [GNU Affero General Public License v3.0](LICENSE). If you run a modified version as a
network service, you must offer its users the source of your version.
