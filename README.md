# Myotis

Myotis is a self-hostable web-based platform made for mail interception, mostly for staging/test environments where you don't want mail going out onto the wider web.

Named for the mouse-eared bats: they catch things in flight, and Myotis catches mail in transit.

## Why another mail catcher

I built this project on top of the shoulders of giants, and I appreciate the inspiration and lessons learned from the projects that came before this.

[smtp4dev](https://github.com/rnwood/smtp4dev) and [Mailpit](https://github.com/axllent/mailpit) are excellent, and
for one developer or one team they're probably all you need. We've used smtp4dev integrated into our software for years and it's made our lives much better as developers, we'd spin one up inside of our local dev intercepting all mail to inspect.

However we had a gap: our staging and test environments. We'd usually just close off e-mail sending, but a client couldn't test some e-mail behaviors, we had a few options we've done before (email rewrite, allowlist specific accounts/domains), but these options required us to re-implement these rules into the software itself, not great.

What we really wanted was just a multi-tenanted smtp4dev, and that's what I've made.



TODO: Rest of the readme (not slop-generated)