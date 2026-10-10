// The OData query a caller hands a broker read member: the condition and order are the caller's,
// and the broker only writes them onto the wire as $filter, $orderby, $skip and $top.
export interface ODataQuery {
    filter?: string;
    orderBy?: string;
    skip?: number;
    top?: number;
}
