# Reads a command line as the Bash tool would run it and prints one fact per
# line about the git commands in it, for git-identity-guard.sh to judge:
#
#   IDENT <tab> <label> <tab> <identity>   an identity the command sets inline
#   HISTORY                                a git command that writes history
#   COMMIT                                 ...and records the configured identity
#
# It reads the line the way a POSIX shell does — quotes, escapes, $(...) and
# backquote substitutions, heredocs, `bash -c` and `eval` — so that an option
# cannot hide in quoting and a quoted value is never mistaken for an option.
# It decides nothing: every identity is judged by .githooks/identity-guard.sh.
#
# POSIX awk only: it runs under mawk, gawk and BSD awk alike.

BEGIN {
    # Words that run the next word as the command.
    split("command builtin exec nohup time nice sudo stdbuf timeout xargs { ! if then else elif while until do", list, " ")
    for (k in list) PREFIX[list[k]] = 1
    # Their options that take a separate value.
    WRAPPER_VALUES["env"] = "u C S P"
    WRAPPER_VALUES["sudo"] = "u g h p C D r t U"
    WRAPPER_VALUES["nice"] = "n"
    WRAPPER_VALUES["timeout"] = "k s"
    WRAPPER_VALUES["xargs"] = "I L n P d E s a"

    HISTORY_COMMANDS = " commit commit-tree push merge pull rebase cherry-pick revert am tag notes filter-branch filter-repo "
    COMMIT_VALUES = "--message --file --reuse-message --reedit-message --fixup --squash --date --template --cleanup --trailer --pathspec-from-file --author"
}

{ S = S $0 "\n" }

END {
    P = 1
    N = length(S)
    parse_list(0)
}

# ---------------------------------------------------------------- the reader

# Reads commands from S at P until the end or, when nested, the ")" that closes
# a $( substitution. Each simple command's words go to examine().
function parse_list(nested,    words, nw, w, inword, c, d, delims, strips, nd) {
    words[0] = ""
    delims[0] = ""
    strips[0] = 0
    nw = 0
    w = ""
    inword = 0
    nd = 0
    while (P <= N) {
        c = substr(S, P, 1)
        if (c == "\\") {
            d = substr(S, P + 1, 1)
            if (d != "\n") { w = w d; inword = 1 }
            P += 2
            continue
        }
        if (c == "'") { w = w single_quoted(); inword = 1; continue }
        if (c == "\"") { w = w double_quoted(); inword = 1; continue }
        if (c == "$" && substr(S, P + 1, 1) == "'") { P++; w = w ansi_quoted(); inword = 1; continue }
        if (c == "$" && substr(S, P + 1, 1) == "(") { w = w substitution(); inword = 1; continue }
        if (c == "`") { w = w backquoted(); inword = 1; continue }
        if (c == " " || c == "\t" || c == "\r") {
            if (inword) words[++nw] = w
            w = ""
            inword = 0
            P++
            continue
        }
        if (c == "#" && !inword) {
            while (P <= N && substr(S, P, 1) != "\n") P++
            continue
        }
        if (c == "<" && substr(S, P + 1, 1) == "<" && substr(S, P + 2, 1) != "<") {
            if (inword) words[++nw] = w
            w = ""
            inword = 0
            P += 2
            strips[++nd] = 0
            if (substr(S, P, 1) == "-") { strips[nd] = 1; P++ }
            while (substr(S, P, 1) == " " || substr(S, P, 1) == "\t") P++
            delims[nd] = heredoc_delimiter()
            continue
        }
        if (index("\n;&|()", c)) {
            if (inword) words[++nw] = w
            w = ""
            inword = 0
            if (nw) examine(words, nw)
            nw = 0
            P++
            if (c == "\n" && nd) { skip_heredocs(delims, strips, nd); nd = 0 }
            if (c == ")" && nested) return
            continue
        }
        w = w c
        inword = 1
        P++
    }
    if (inword) words[++nw] = w
    if (nw) examine(words, nw)
}

# Reads a whole command line held in a string: bash -c, eval, backquotes.
function parse_string(text,    s0, p0, n0) {
    s0 = S; p0 = P; n0 = N
    S = text "\n"
    P = 1
    N = length(S)
    parse_list(0)
    S = s0; P = p0; N = n0
}

function single_quoted(    k, v) {
    k = index(substr(S, P + 1), "'")
    if (!k) {
        v = substr(S, P + 1)
        P = N + 1
        return v
    }
    v = substr(S, P + 1, k - 1)
    P += k + 1
    return v
}

function double_quoted(    v, c, d) {
    v = ""
    P++
    while (P <= N) {
        c = substr(S, P, 1)
        if (c == "\"") { P++; return v }
        if (c == "\\") {
            d = substr(S, P + 1, 1)
            if (index("\"\\$`", d)) v = v d
            else if (d != "\n") v = v c d
            P += 2
            continue
        }
        if (c == "$" && substr(S, P + 1, 1) == "(") { v = v substitution(); continue }
        if (c == "`") { v = v backquoted(); continue }
        v = v c
        P++
    }
    return v
}

function ansi_quoted(    v, c, d) {
    v = ""
    P++
    while (P <= N) {
        c = substr(S, P, 1)
        if (c == "'") { P++; return v }
        if (c == "\\") {
            d = substr(S, P + 1, 1)
            if (d == "n") v = v "\n"
            else if (d == "t") v = v "\t"
            else v = v d
            P += 2
            continue
        }
        v = v c
        P++
    }
    return v
}

# $(...) runs its own commands, which are examined too; $((...)) is arithmetic.
function substitution(    depth, c) {
    if (substr(S, P + 2, 1) == "(") {
        P += 3
        depth = 2
        while (P <= N && depth) {
            c = substr(S, P, 1)
            if (c == "(") depth++
            else if (c == ")") depth--
            P++
        }
        return "0"
    }
    P += 2
    parse_list(1)
    return "$(...)"
}

function backquoted(    v, c) {
    v = ""
    P++
    while (P <= N) {
        c = substr(S, P, 1)
        if (c == "`") { P++; break }
        if (c == "\\") { v = v substr(S, P + 1, 1); P += 2; continue }
        v = v c
        P++
    }
    parse_string(v)
    return "`...`"
}

function heredoc_delimiter(    w, c) {
    w = ""
    while (P <= N) {
        c = substr(S, P, 1)
        if (c == "'") { w = w single_quoted(); continue }
        if (c == "\"") { w = w double_quoted(); continue }
        if (c == "\\") { w = w substr(S, P + 1, 1); P += 2; continue }
        if (index(" \t\n;&|()<>", c)) break
        w = w c
        P++
    }
    return w
}

# A heredoc's body is input, not commands: skip to each delimiter line in turn.
function skip_heredocs(delims, strips, nd,    k, e, line) {
    for (k = 1; k <= nd; k++) {
        while (P <= N) {
            e = index(substr(S, P), "\n")
            if (e) { line = substr(S, P, e - 1); P += e }
            else { line = substr(S, P); P = N + 1 }
            if (strips[k]) sub(/^\t+/, "", line)
            sub(/\r$/, "", line)
            if (line == delims[k]) break
        }
    }
}

# ------------------------------------------------------------- the commands

function examine(words, nw,    i, t, name) {
    i = 1
    while (i <= nw) {
        t = words[i]
        if (is_assignment(t)) { assign(t); i++; continue }
        name = command_name(t)
        if (name == "export" || name == "declare" || name == "typeset" || name == "local" || name == "readonly") {
            for (i++; i <= nw; i++) if (is_assignment(words[i])) assign(words[i])
            return
        }
        if (name == "env" || name in PREFIX) {
            i = skip_wrapper_options(name, words, i + 1, nw)
            continue
        }
        break
    }
    if (i > nw) return
    name = command_name(words[i])
    if (name == "git") git_command(words, i + 1, nw)
    else if (name == "eval") parse_string(join(words, i + 1, nw))
    else if (name ~ /^(ba|z|da|k)?sh$/ || name == "pwsh" || name == "powershell") shell_command(words, i + 1, nw)
}

function command_name(t) {
    sub(/.*[\/\\]/, "", t)
    t = tolower(t)
    sub(/\.exe$/, "", t)
    return t
}

function skip_wrapper_options(name, words, i, nw,    t, values) {
    values = " " WRAPPER_VALUES[name] " "
    while (i <= nw) {
        t = words[i]
        if (name == "env" && is_assignment(t)) { assign(t); i++; continue }
        if (t !~ /^-/) break
        i++
        if (length(t) == 2 && index(values, " " substr(t, 2, 1) " ")) i++
    }
    if (name == "timeout" && i <= nw) i++
    return i
}

# bash -c '...', pwsh -Command '...': the string is itself a command line.
function shell_command(words, i, nw,    t) {
    for (; i <= nw; i++) {
        t = tolower(words[i])
        if (t ~ /^-[a-z]*c$/ || t == "-command") {
            if (i + 1 <= nw) parse_string(words[i + 1])
            return
        }
        if (t !~ /^-/) return
    }
}

function join(words, i, nw,    out) {
    out = ""
    for (; i <= nw; i++) out = out (out == "" ? "" : " ") words[i]
    return out
}

function is_assignment(t) {
    return t ~ /^(\$env:)?[A-Za-z_][A-Za-z0-9_]*=/
}

function assign(t,    eq, name, value, upper, n) {
    eq = index(t, "=")
    name = substr(t, 1, eq - 1)
    sub(/^\$env:/, "", name)
    value = substr(t, eq + 1)
    ENV_VALUES[name] = value
    upper = toupper(name)
    if (upper == "GIT_AUTHOR_NAME" || upper == "GIT_COMMITTER_NAME") ident_name(upper, value)
    else if (upper == "GIT_AUTHOR_EMAIL" || upper == "GIT_COMMITTER_EMAIL") ident_email(upper, value)
    else if (upper ~ /^GIT_CONFIG_KEY_[0-9]+$/) {
        n = substr(upper, 16)
        CONFIG_KEYS[n] = value
        if (n in CONFIG_VALUES) config_pair(value, CONFIG_VALUES[n], 1)
    }
    else if (upper ~ /^GIT_CONFIG_VALUE_[0-9]+$/) {
        n = substr(upper, 18)
        CONFIG_VALUES[n] = value
        if (n in CONFIG_KEYS) config_pair(CONFIG_KEYS[n], value, 1)
    }
    else if (upper == "GIT_CONFIG_PARAMETERS") config_parameters(value)
}

# GIT_CONFIG_PARAMETERS holds 'key'='value' pairs, or older 'key=value' ones.
function config_parameters(v,    pair, eq) {
    while (match(v, /'[^']*'(='[^']*')?/)) {
        pair = substr(v, RSTART, RLENGTH)
        v = substr(v, RSTART + RLENGTH)
        if (index(pair, "'='")) {
            eq = index(pair, "'='")
            config_pair(substr(pair, 2, eq - 2), substr(pair, eq + 3, length(pair) - eq - 3), 1)
        } else {
            config_argument(substr(pair, 2, length(pair) - 2))
        }
    }
}

# -c name=value
function config_argument(kv,    eq) {
    eq = index(kv, "=")
    if (eq) config_pair(substr(kv, 1, eq - 1), substr(kv, eq + 1), 1)
    else config_pair(kv, "", 0)
}

# --config-env=name=envvar
function config_environment(kv,    eq, variable) {
    eq = index(kv, "=")
    if (!eq) return
    variable = substr(kv, eq + 1)
    if (variable in ENV_VALUES) config_pair(substr(kv, 1, eq - 1), ENV_VALUES[variable], 1)
    else config_pair(substr(kv, 1, eq - 1), "", 0)
}

function config_pair(key, value, has_value,    lower) {
    lower = tolower(key)
    if (!has_value) return
    if (lower ~ /^(user|author|committer)\.name$/) ident_name(key, value)
    else if (lower ~ /^(user|author|committer)\.email$/) ident_email(key, value)
}

function git_command(words, i, nw,    t, sub_command) {
    while (i <= nw) {
        t = words[i]
        if (t == "-c") { if (i < nw) config_argument(words[i + 1]); i += 2; continue }
        if (t == "--config-env") { if (i < nw) config_environment(words[i + 1]); i += 2; continue }
        if (t ~ /^--config-env=/) { config_environment(substr(t, 14)); i++; continue }
        if (t == "-C" || t == "--git-dir" || t == "--work-tree" || t == "--namespace" || t == "--attr-source") { i += 2; continue }
        if (t ~ /^-/) { i++; continue }
        break
    }
    if (i > nw) return
    sub_command = tolower(words[i])
    i++
    if (sub_command == "config") config_command(words, i, nw)
    if (index(HISTORY_COMMANDS, " " sub_command " ")) {
        print "HISTORY"
        print "COMMIT"
    }
    if (sub_command == "commit") commit_options(words, i, nw)
}

function commit_options(words, i, nw,    t, eq, name, k, c) {
    while (i <= nw) {
        t = words[i]
        i++
        if (t == "--") return
        if (t ~ /^--/) {
            eq = index(t, "=")
            name = eq ? substr(t, 1, eq - 1) : t
            if (is_prefix(name, "--author", 4)) {
                if (eq) author(substr(t, eq + 1))
                else if (i <= nw) author(words[i++])
                continue
            }
            if (!eq && takes_value(name, COMMIT_VALUES)) i++
            continue
        }
        if (t ~ /^-./) {
            for (k = 2; k <= length(t); k++) {
                c = substr(t, k, 1)
                if (index("mFCct", c)) { if (k == length(t)) i++; break }
                if (index("Su", c)) break
            }
        }
    }
}

function config_command(words, i, nw,    t, n, positional) {
    n = 0
    positional[0] = ""
    while (i <= nw) {
        t = words[i]
        i++
        if (t == "-f" || t == "--file" || t == "--blob" || t == "--type" || t == "--default" || t == "--comment") { i++; continue }
        if (t ~ /^-/) continue
        positional[++n] = t
    }
    if (n && (positional[1] == "set" || positional[1] == "unset")) {
        for (i = 1; i < n; i++) positional[i] = positional[i + 1]
        n--
    }
    if (n >= 2) config_pair(positional[1], positional[2], 1)
}

function is_prefix(name, full, shortest) {
    return length(name) >= shortest && substr(full, 1, length(name)) == name
}

function takes_value(name, options,    list, count, k) {
    count = split(options, list, " ")
    for (k = 1; k <= count; k++) if (is_prefix(name, list[k], 4)) return 1
    return 0
}

# --author takes "Name <email>", or a name git looks up among past authors.
function author(value) {
    if (index(value, "<")) emit_ident("--author", value)
    else ident_name("--author", value)
}

function ident_name(label, value) { emit_ident(label, value " <>") }
function ident_email(label, value) { emit_ident(label, "<" value ">") }

function emit_ident(label, value) {
    gsub(/[\t\r\n]/, " ", value)
    print "IDENT\t" label "\t" value
}
