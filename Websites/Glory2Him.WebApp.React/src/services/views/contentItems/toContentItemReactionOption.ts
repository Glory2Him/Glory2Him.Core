import { Reaction } from '../../../models/foundations/reactions/reaction';

import {
    ContentItemReactionOption
} from '../../../models/components/contentItems/contentItemSearchItem';

// The projection between the reaction vocabulary off the wire and the choices the Like control
// offers — shared by every page that feeds the search panel family, so "which one is Love"
// (the option a LimitReactionsToLoveOnly setting keeps, §6.5) is decided once.
//
// The isLove match is on the NAME, case-insensitively, because the vocabulary rows carry no flag
// of their own. That makes renaming the Love reaction a decision with a consequence — a rename
// leaves love-only types offering nothing — which is why it is documented here rather than
// hidden: when the vocabulary grows its own flag, this line is the only one that changes.
export const toContentItemReactionOption = (reaction: Reaction): ContentItemReactionOption => ({
    label: reaction.name,
    glyph: reaction.unicodeEmoji,
    isLove: reaction.name.trim().toLowerCase() === 'love'
});
