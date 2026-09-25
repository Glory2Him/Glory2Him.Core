# Prints the decoded value of every JSON string held under one of the keys in
# `keys` (space-separated, passed with -v), each followed by a newline, in the
# order they appear. Values under other keys, and object or array values, are
# skipped. git-identity-guard.sh reads Claude Code's hook payloads with it, so
# that only the fields it means to judge are judged.
#
# POSIX awk only: it runs under mawk, gawk and BSD awk alike.

BEGIN {
    count = split(keys, list, " ")
    for (k = 1; k <= count; k++) wanted[list[k]] = 1
}

{ text = text $0 "\n" }

END {
    len = length(text)
    i = 1
    key = ""
    while (i <= len) {
        c = substr(text, i, 1)
        if (c == "\"") {
            value = read_string()
            j = i
            while (j <= len && index(" \t\r\n", substr(text, j, 1))) j++
            if (substr(text, j, 1) == ":") {
                key = value
                i = j + 1
                continue
            }
            if (key in wanted) printf "%s\n", value
            key = ""
            continue
        }
        if (c == "{" || c == "[" || c == ",") key = ""
        i++
    }
}

# Reads the string that opens at text[i] and leaves i just past its closing quote.
function read_string(    out, rest, c) {
    out = ""
    i++
    while (i <= len) {
        rest = substr(text, i)
        if (!match(rest, /["\\]/)) {
            out = out rest
            i = len + 1
            break
        }
        out = out substr(rest, 1, RSTART - 1)
        i += RSTART - 1
        if (substr(text, i, 1) == "\"") {
            i++
            break
        }
        c = substr(text, i + 1, 1)
        if (c == "n") out = out "\n"
        else if (c == "t") out = out "\t"
        else if (c == "r") out = out "\r"
        else if (c == "b" || c == "f") out = out " "
        else if (c == "u") {
            out = out code_point(substr(text, i + 2, 4))
            i += 4
        }
        else out = out c
        i += 2
    }
    return out
}

function code_point(hex,    value, k, digit) {
    value = 0
    for (k = 1; k <= 4; k++) {
        digit = index("0123456789abcdef", tolower(substr(hex, k, 1)))
        if (!digit) return "?"
        value = value * 16 + digit - 1
    }
    if (value == 9) return "\t"
    if (value == 10) return "\n"
    if (value == 13) return "\r"
    if (value >= 32 && value < 127) return sprintf("%c", value)
    return "?"
}
