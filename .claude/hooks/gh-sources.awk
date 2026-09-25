# Reads a shell command's text and prints what pre-bash needs in order to judge
# the gh commands in it that write GitHub text (Architecture.md §ARC12.11 rule 5).
# Both the commands and the options that carry text are closed lists:
#
#   gh pr create|edit|comment|review|merge, gh issue create|edit|comment,
#   gh release create|edit, and gh api when it passes a body or title field or
#   uses --input. gh may be path-prefixed.
#
#   inline: --body -b --title -t --notes, -n on release, and a gh api field
#           (-f --raw-field, or -F --field whose value does not start with @)
#   file:   --body-file --notes-file --input, -F on pr, issue and release, and
#           a gh api -F --field value that starts with @
#
# Each option is read as "--option value", "--option=value", "-x value" or
# "-xvalue". For every text-writing gh command it prints "GH", then a line per
# text source, "INLINE<tab>option<tab>value" or "FILE<tab>option<tab>path", with
# any tab or newline in the value printed as a space.
#
# It reads words, quotes, escapes and command separators, and nothing else of the
# shell's grammar. Set shell=powershell (with -v) for PowerShell, whose escape
# character is the backtick. POSIX awk only, and linear in the command's length
# under mawk, gawk and BSD awk: a word is built in bounded chunks, never a
# character at a time onto the whole.

BEGIN {
    esc = (shell == "powershell") ? "`" : "\\"
    count = 0
    quote = ""
    pending = 0
    start_word()
}

{
    n = split($0, ch, "")
    ch[n + 1] = "\n"
    for (i = 1; i <= n + 1; i++) read_char(ch[i], ch[i + 1], i > 1 ? ch[i - 1] : "")
}

END {
    end_word()
    for (w = 1; w <= count; w++) {
        if (sep[w] || !is_gh(word[w])) continue
        group = word[w + 1]
        action = word[w + 2]
        if (group == "api") read_api(w + 2)
        else if ((group == "pr" && action ~ /^(create|edit|comment|review|merge)$/) ||
            (group == "issue" && action ~ /^(create|edit|comment)$/) ||
            (group == "release" && action ~ /^(create|edit)$/)) read_text_command(w + 3, group)
    }
}

# ------------------------------------------------------------------ the words

function start_word() {
    in_word = 0
    buffer = ""
    buffered = 0
    current = ""
}

function add(c) {
    in_word = 1
    buffer = buffer c
    if (++buffered >= 256) {
        current = current buffer
        buffer = ""
        buffered = 0
    }
}

function end_word() {
    if (in_word) {
        word[++count] = current buffer
        sep[count] = 0
    }
    start_word()
}

function separator() {
    end_word()
    word[++count] = ""
    sep[count] = 1
}

function read_char(c, next_c, previous_c) {
    if (pending) {
        pending = 0
        if (c != "\n") add(c)
        return
    }
    if (quote == "'") {
        if (c == "'") quote = ""
        else add(c)
        return
    }
    if (quote == "\"") {
        if (c == "\"") quote = ""
        else if (c == esc && (esc == "`" || index("\"\\$`\n", next_c))) pending = 1
        else add(c)
        return
    }
    if (c == " " || c == "\t" || c == "\r") { end_word(); return }
    if (c == "\n" || c == ";" || c == "|") { separator(); return }
    if (c == "&") {
        if (previous_c == ">" || previous_c == "<" || next_c == ">") add(c)
        else separator()
        return
    }
    if (c == "'" || c == "\"") { quote = c; in_word = 1; return }
    if (c == esc) { pending = 1; return }
    add(c)
}

function is_gh(w) {
    return w == "gh" || w ~ /[\/\\]gh$/
}

# --------------------------------------------------------------- the sources

function emit(kind, option, value) {
    gsub(/[\t\n]/, " ", value)
    printf "%s\t%s\t%s\n", kind, option, value
}

# The value of the option in word[k]: attached after "=" (long) or after the
# flag (short), or else the next word. Leaves k on the last word it read.
function value_of(flag) {
    if (substr(word[k], 1, 2) == "--") {
        if (index(word[k], "=")) return substr(word[k], index(word[k], "=") + 1)
    }
    else if (length(word[k]) > length(flag)) return substr(word[k], length(flag) + 1)
    k++
    return sep[k] ? "" : word[k]
}

function option_name(w) {
    if (substr(w, 1, 2) == "--") return index(w, "=") ? substr(w, 1, index(w, "=") - 1) : w
    return substr(w, 1, 2)
}

function read_text_command(from, group,    total, kinds, options, values, name) {
    total = 0
    for (k = from; k <= count && !sep[k]; k++) {
        name = option_name(word[k])
        if (name == "--body" || name == "-b" || name == "--title" || name == "-t" ||
            name == "--notes" || (name == "-n" && group == "release")) {
            kinds[++total] = "INLINE"
        }
        else if (name == "--body-file" || name == "--notes-file" || name == "-F") {
            kinds[++total] = "FILE"
        }
        else continue
        options[total] = name
        values[total] = value_of(name)
    }
    print "GH"
    for (s = 1; s <= total; s++) emit(kinds[s], options[s], values[s])
}

function read_api(from,    total, kinds, options, values, name, field, key, writes) {
    total = 0
    writes = 0
    for (k = from; k <= count && !sep[k]; k++) {
        name = option_name(word[k])
        if (name == "--input") {
            kinds[++total] = "FILE"
            options[total] = name
            values[total] = value_of(name)
            writes = 1
            continue
        }
        if (name != "-f" && name != "--raw-field" && name != "-F" && name != "--field") continue
        field = value_of(name)
        key = index(field, "=") ? substr(field, 1, index(field, "=") - 1) : field
        if (key == "body" || key == "title") writes = 1
        options[++total] = name
        values[total] = substr(field, length(key) + 2)
        kinds[total] = "INLINE"
        if ((name == "-F" || name == "--field") && substr(values[total], 1, 1) == "@") {
            kinds[total] = "FILE"
            values[total] = substr(values[total], 2)
        }
    }
    if (!writes) return
    print "GH"
    for (s = 1; s <= total; s++) emit(kinds[s], options[s], values[s])
}
