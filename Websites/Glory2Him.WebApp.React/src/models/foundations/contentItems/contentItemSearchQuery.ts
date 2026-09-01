import { ApprovalStatus } from '../../components/associations/associationItem';
import { ContentItem } from './contentItem';
import { ContentType } from '../contentItemSettings/contentType';
import { ShareabilityBasis } from '../../components/contentItems/contentItemFormItem';

// What a page of the content-item list asks for. The sibling of ContentItemSettingQuery, and
// paged the same way for the same reason.
//
// api/ContentItems is an ordinary MVC route carrying [EnableQuery], not an OData route, so there
// is no @odata.count and $count adds no total. A page therefore asks for ONE ROW BEYOND the page
// and drops it: the extra row is the only thing that separates a full last page from a page with
// more behind it.
export type ContentItemSearchQuery = {
    // WHICH read serves the page, and it is the page's decision. 'public' is
    // GET api/ContentItems/Public — caller-INDEPENDENT, exactly the §14.1 canonical set, so no
    // role change elsewhere can leak a draft onto a surface built on it. 'caller' is
    // GET api/ContentItems, which widens with whoever is asking: their own rows, and everything
    // a review role covers.
    scope: 'public' | 'caller';

    // Free text, matched server-side against the title, the content and the author.
    searchTerm: string;

    // Null is "any category".
    contentType: ContentType | null;

    // The author of the WORDS, matched as a substring — a surname or a first name has to be
    // enough to find someone.
    author: string;

    // The submitter — CreatedBy, matched exactly: it is an account id, and ids are not searched
    // by fragment. Null asks for everybody's.
    submittedById: string | null;

    // Null is "any shareability". The member NAME travels in $filter, like the statuses.
    shareabilityBasis: ShareabilityBasis | null;

    // Narrows to these statuses — the moderation queue's Draft + Submitted. Null or empty leaves
    // the read's own visibility rules to decide alone. The names travel as MEMBER NAMES in
    // $filter while JSON bodies carry the numbers, the same split ContentType has.
    approvalStatuses: ReadonlyArray<ApprovalStatus> | null;

    // Zero-based, because it is the react-query page param and arithmetic on it reads better
    // from zero. ContentItemSettingQuery counts from one because an admin table shows the number.
    pageIndex: number;

    // The host caps [EnableQuery] reads at OData:PageSize (50), and the +1 probe row rides inside
    // that cap, so anything approaching it would silently lose the probe and never page again.
    pageSize: number;
};

export type ContentItemPage = {
    items: ContentItem[];
    pageIndex: number;
    pageSize: number;
    hasNextPage: boolean;
};
