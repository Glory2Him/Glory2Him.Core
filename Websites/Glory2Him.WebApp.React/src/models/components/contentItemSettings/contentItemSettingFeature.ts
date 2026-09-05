import {
    ContentItemSetting
} from '../../foundations/contentItemSettings/contentItemSetting';

// Only the setting's boolean members can be wired to a switch, so a mistyped field name in the
// table below is a compile error rather than a switch that silently never moves.
// -? strips the optional modifier, or every optional member (the Max*Length ceilings) would
// smuggle `undefined` into the union and no field name would satisfy it.
export type ContentItemSettingFlag = {
    [TField in keyof ContentItemSetting]-?:
    ContentItemSetting[TField] extends boolean ? TField : never
}[keyof ContentItemSetting];

// Each pair is one of design §6.10's resolved features: "shown" governs whether it renders at
// all, "allowed" whether something new can be created against a content item of this type. They
// are independent — a closed comment thread that still displays its history sets shown on and
// allowed off, which is why shown reads first here and everywhere the pairs are rendered.
export type ContentItemSettingFeatureField = {
    title: string;
    allowedLabel: string;
    shownLabel: string;
    allowed: ContentItemSettingFlag;
    shown: ContentItemSettingFlag;
};

// THE ONE TABLE, shared by every surface that renders the feature pairs — the admin detail page's
// wide three-column card and the settings panel's narrow stacked column alike. What must not
// drift between them is which field carries which label and which two fields pair up; how many
// bootstrap columns a surface spends on that is its own business, and a sidebar and a full page
// legitimately answer it differently.
export const contentItemSettingFeatureFields: ReadonlyArray<ContentItemSettingFeatureField> = [
    {
        title: 'Tags',
        allowedLabel: 'Tags can be added',
        shownLabel: 'Tags are shown',
        allowed: 'tagsAllowed',
        shown: 'showTags',
    },
    {
        title: 'Reactions',
        allowedLabel: 'Reactions can be added',
        shownLabel: 'Reactions are shown',
        allowed: 'reactionsAllowed',
        shown: 'showReactions',
    },
    {
        title: 'Links',
        allowedLabel: 'Links can be added',
        shownLabel: 'Links are shown',
        allowed: 'linksAllowed',
        shown: 'showLinks',
    },
    {
        title: 'Attachments',
        allowedLabel: 'Attachments can be added',
        shownLabel: 'Attachments are shown',
        allowed: 'attachmentsAllowed',
        shown: 'showAttachments',
    },
    {
        title: 'Comments',
        allowedLabel: 'Comments can be added',
        shownLabel: 'Comments are shown',
        allowed: 'commentsAllowed',
        shown: 'showComments',
    },
    {
        title: 'Bible references',
        allowedLabel: 'Bible references can be added',
        shownLabel: 'Bible references are shown',
        allowed: 'bibleReferenceAllowed',
        shown: 'showBibleReferences',
    },
];

// The love-only switch stands apart from the pairs: it is not a shown/allowed pair but a
// narrowing of WHICH reaction may be associated, so it renders below the table with its own
// explanation rather than as a seventh row.
export const limitReactionsToLoveOnlyLabel = 'Limit reactions to love only';

export const limitReactionsToLoveOnlyDescription =
    'Favourite-style behaviour: only the designated love reaction may be associated with '
    + 'content items of this type.';
