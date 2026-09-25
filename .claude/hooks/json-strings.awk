# Prints the decoded value of every JSON string held under one of the keys in
# `keys` (space-separated, passed with -v), each followed by a newline, in the
# order they appear. Values under other keys, and object or array values, are
# skipped. git-identity-guard.sh reads Claude Code's hook payloads with it, so
# that only the fields it means to judge are judged.
#
# POSIX awk only: it runs under mawk, gawk and BSD awk alike, and in time linear
# in the payload's length under each. The payload is never walked a character at
# a time: escaped backslashes and quotes are set aside with gsub, and the rest is
# split once on its quotes, so every even-numbered part is a string's contents.

BEGIN {
    RS = "\001"
    count = split(keys, list, " ")
    for (k = 1; k <= count; k++) wanted[list[k]] = 1
}

{ text = text $0 }

END {
    # A JSON string holds no raw control character, so these two cannot collide.
    gsub(/\\\\/, "\001", text)
    gsub(/\\"/, "\002", text)
    parts = split(text, part, /"/)
    key = ""
    for (p = 2; p <= parts; p += 2) {
        after = part[p + 1]
        if (after ~ /^[ \t\r\n]*:/) {
            # A key: it names the next value only if that value is a string.
            key = (after ~ /^[ \t\r\n]*:[ \t\r\n]*$/) ? part[p] : ""
            continue
        }
        if (key in wanted) print_decoded(part[p])
        key = ""
    }
}

# Prints a string's contents, decoded, and a newline.
function print_decoded(s,    pieces, piece, k) {
    gsub(/\\n/, "\n", s)
    gsub(/\\t/, "\t", s)
    gsub(/\\r/, "\r", s)
    gsub(/\\[bf]/, " ", s)
    gsub(/\\\//, "/", s)
    gsub(/\002/, "\"", s)
    pieces = split(s, piece, /\\u/)
    print_restored(piece[1])
    for (k = 2; k <= pieces; k++) {
        printf "%s", code_point(substr(piece[k], 1, 4))
        print_restored(substr(piece[k], 5))
    }
    printf "\n"
}

# Prints s with each escaped backslash, set aside as \001, restored.
function print_restored(s,    pieces, piece, k) {
    pieces = split(s, piece, /\001/)
    printf "%s", piece[1]
    for (k = 2; k <= pieces; k++) printf "\\%s", piece[k]
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
