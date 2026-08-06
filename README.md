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

To bring a database up to date **from the application**, EF applies the migrations itself and
nothing special is needed:

```bash
dotnet ef database update --project Glory2Him.Core
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

**Why the tool and not `dotnet ef migrations script` on its own.** The schema uses filtered
indexes and indexes on computed columns, and SQL Server refuses to create those unless a
specific set of `SET` options is on. EF emits no `SET` statements, and `sqlcmd` defaults
`QUOTED_IDENTIFIER` to **off** — so the raw script fails on its very first `CREATE INDEX` with
`Msg 1934`, and not one migration is applied. The tool prepends the required options to the
generated file, so the artifact is correct however it is applied instead of depending on
whoever runs it remembering `sqlcmd -I`.

---

