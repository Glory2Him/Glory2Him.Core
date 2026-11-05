// A suggested tag, named as the reader typed it, for one content item. Whether the name is a
// tag, and which, is the server's to settle (TagSuggestions.md rule 2).
export interface TagSuggestion {
    contentItemId: string;
    name: string;
}
