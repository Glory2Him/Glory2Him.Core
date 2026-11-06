# Glory2Him.Core
![Glory 2 Him](https://raw.githubusercontent.com/Glory2Him/Glory2Him/main/Resources/Images/Glory2Him-Banner.png)
---

## ✝️ Introduction  

**Glory 2 Him** creates software to connect people with God, offering digital tools and resources  
that bring faith into *everyday life*.  

Our mission is to **encourage and equip every believer** on their journey of faith through  
open-source software, tools, and libraries that we develop.  

Join our *community of developers and designers*—or, if you don’t have technical skills but  
see a **digital need**, share it with us. Together, we can discover new ways to serve the  
**body of Christ** in meaningful and lasting ways.  

---

## 🌄 What is Glory2Him.Core?  

**Glory2Him.Core** is a collaborative portal system where anyone can contribute content 
with a Christian theme.  It offers a wide range of resources organized by topic—such as 
*inspirational quotes*, *Bible verses*, *images*, and links to trusted Christian resources 
like **sermons** or **books**.  

All content submitted is moderated and subject to approval to ensure it is to the 
**glory of God**, the **building of His Kingdom**, uplifting, and a blessing to people 
in line with Christian values.  

**Key Features:**  
- 💬 *Inspirational Quotes*  
- 🌟 *Inspirational Stories / Testimonies*  
- 📖 *Bible Verses*  
- 🖼️ *Backgrounds*  
- ✨ *Verse Images*  
- 🛡️ *Moderated for Quality & Faithfulness*  
- 🌍 *Social Media Integration* (Twitter / Instagram / Facebook / WhatsApp / Telegram)  

---

## 🗄️ Database migrations

`--context` is required: Glory2Him.Core carries two DbContexts — `StorageBroker` for its own
database and the read-only `IdentityCoreStorageBroker` over the security database (design
§12.7.1) — so EF will not guess which one you mean.

To bring a database up to date, EF applies the migrations itself — name the Core context and
nothing else is needed:

```bash
dotnet ef database update --project Glory2Him.Core --context StorageBroker
```

To produce a **SQL script** for a DBA or a deployment pipeline, use the tool rather than
`dotnet ef migrations script` directly:

```bash
Tools/new-database-script.sh Glory2Him.Core.Database.sql
```

Then apply it:

```bash
sqlcmd -S <server> -d <database> -i Glory2Him.Core.Database.sql -b
```

The script is idempotent — applying it to an up-to-date database is a no-op, and applying it
to an empty one builds the whole schema.

**Keep the `-b`.** Without it `sqlcmd` prints the error and still exits `0`, so a pipeline step
reports success over a database that received nothing. Each migration is its own transaction,
so a failure leaves the database consistent — but silently un-migrated.

**Running your own SQL against this schema needs `sqlcmd -I`.** The header above covers the
generated script, not your session. `Associations` carries indexes on computed columns, and any
`INSERT` or `UPDATE` against it under `sqlcmd`'s default `QUOTED_IDENTIFIER OFF` fails with the
same `Msg 1934` — so backfills, data fixes and seed scripts need `-I` (or the same `SET` header)
even though the schema is already in place.

**Why the tool and not `dotnet ef migrations script` on its own.** The schema uses filtered
indexes and indexes on computed columns, and SQL Server refuses to create those unless a
specific set of `SET` options is on. EF emits no `SET` statements, and `sqlcmd` defaults
`QUOTED_IDENTIFIER` to **off** — so the raw script fails on its very first `CREATE INDEX` with
`Msg 1934`, and not one migration is applied. The tool prepends the required options to the
generated file, so the artifact is correct however it is applied instead of depending on
whoever runs it remembering `sqlcmd -I`.

---

## 🛠️ Getting started

You need the .NET 10 SDK, Node 22 and LocalDB. From the repository root:

```powershell
dotnet build
dotnet test Glory2Him.Core.Tests.Unit
```

`DEVELOPERS.md` covers the rest: every test suite, the React app, the agents that plan,
build and verify the work, and how a release is made.

---

## 🤝 Contributing

Work starts as a GitHub issue: a **Feature request** raised through its form, planned
into user stories and tasks, and built task by task. `DEVELOPERS.md` walks through it.
Pull request titles must start with one of the category prefixes listed in
`.github/workflows/prLinter.yml` (for example `MINOR FOUNDATIONS:` or `DOCUMENTATION:`),
and the description must link an issue with `Closes #<n>`. Both are enforced by CI, and
so is the rule that every commit is made under a person's own identity, with no AI
attribution.

---

## 📜 License

Licensed under the Glory 2 Him Software License (G2HSL). Two files, deliberately:
[LICENSE.txt](LICENSE.txt) is this repository's copy — the licence with the
repository's own name and copyright line on top — and [G2HSL.md](G2HSL.md) is the
licence on its own, unmodified. The canonical copy lives at
[Glory2Him/Glory2Him](https://github.com/Glory2Him/Glory2Him/blob/main/G2HSL.md).

**FREE TO USE TO HELP SHARE THE GOSPEL**

> John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
> No one comes to the Father except through me.'"
> [john.bible/john-14-6](https://john.bible/john-14-6)

If Jesus is who He said He is, what does that mean for you, today?
